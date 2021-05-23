using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using OfficeOpenXml;

namespace CodeChefRanklistTool
{
    public enum VjudgeOverflowBehavior
    {
        Truncate,
        Sample
    }

    public class VjudgeExporter
    {
        // this is a vjudge limitation
        private const int MaxParticipantCount = 2000;

        private readonly VjudgeOverflowBehavior _overflowBehavior;
        private readonly TimeSpan _penaltyTime;

        public VjudgeExporter(VjudgeOverflowBehavior overflowBehavior, TimeSpan penaltyTime)
        {
            _overflowBehavior = overflowBehavior;
            _penaltyTime = penaltyTime;
        }

        public void Export(RanklistResponse response, string dirPath)
        {
            var unscoredProblems = new HashSet<string>(response.Contest.UnscoredProblems);
            var scoredProblems = response.Problems
                .Where(problem => !unscoredProblems.Contains(problem.Code)).ToList();
            var problemColumns = new Dictionary<string, int>();
            for (var i = 0; i < scoredProblems.Count; i++) problemColumns[scoredProblems[i].Code] = i + 2;

            ExportInfo(response, Path.Combine(dirPath, "info.txt"), scoredProblems);
            ExportXls(response, Path.Combine(dirPath, "ranklist.xlsx"), problemColumns);
        }

        private void ExportInfo(RanklistResponse response, string outFile, List<Problem> orderedProblems)
        {
            Debug.Assert(response.Contest.ContestTimeSpan.Start != null,
                "response.Contest.ContestTimeSpan.Start != null");
            Debug.Assert(response.Contest.ContestTimeSpan.End != null, "response.Contest.ContestTimeSpan.End != null");

            var beginTimeUtc = DateTimeOffset.FromUnixTimeSeconds(response.Contest.ContestTimeSpan.Start.Value);
            var beginTimeLocal = beginTimeUtc.ToLocalTime();
            var length = TimeSpan.FromSeconds(response.Contest.ContestTimeSpan.End.Value -
                                              response.Contest.ContestTimeSpan.Start.Value);

            var contents = new List<string>
            {
                $"Title: CodeChef {response.Contest.DisplayName}",
                $"Begin Time: {beginTimeLocal:yyyy-MM-dd HH:mm:ss}",
                $"Length: {length:c}",
                "Rank rule: Customized",
                "Total penalty: Sum",
                $"Penalty(s): {_penaltyTime.TotalSeconds}",
                "Partial Score: Disable",
                $"Description: CodeChef contest {response.Contest.DisplayName} ({response.Contest.ContestCode}) as it " +
                "was originally held. Problems are in the order they are displayed in the CodeChef ranklist, not " +
                "necessarily in the order of difficulty. Only problems that were rated for this division are included. "
            };

            Debug.Assert(response.TotalItems != null, "response.TotalItems != null");
            if (response.Participants.Count > MaxParticipantCount)
                switch (_overflowBehavior)
                {
                    case VjudgeOverflowBehavior.Sample:
                        contents.Add(
                            "The ranklist is a RANDOM SAMPLE of all participants. The actual number of participants was " +
                            $"{response.TotalItems.Value}, some {MaxParticipantCount} are shown.");
                        break;
                    case VjudgeOverflowBehavior.Truncate:
                        contents.Add("The ranklist is TRUNCATED. The actual number of participants was " +
                                     $"{response.TotalItems.Value}, top {MaxParticipantCount} are shown.");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_overflowBehavior));
                }

            contents.Add(string.Empty);
            contents.Add("Enter problems in THIS order:");
            foreach (var problem in orderedProblems) contents.Add($"{problem.Code} | {problem.DisplayName}");

            File.WriteAllLines(outFile, contents, Encoding.UTF8);
        }

        private void ExportXls(RanklistResponse response, string outFile, IDictionary<string, int> problemColumns)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var excelPackage = new ExcelPackage();
            var worksheet = excelPackage.Workbook.Worksheets.Add("ranklist");

            var maybeReducedList = ReduceParticipantList(response.Participants);
            for (var i = 0; i < maybeReducedList.Count; i++)
                ExportParticipant(worksheet, i + 1, maybeReducedList[i], problemColumns);

            excelPackage.SaveAs(new FileInfo(outFile));
        }

        /// <summary>
        ///     If the number of participants is more than 2000, removes excess according to the specified
        ///     <see cref="VjudgeOverflowBehavior" />.
        /// </summary>
        private List<Participant> ReduceParticipantList(List<Participant> participants)
        {
            if (participants.Count < MaxParticipantCount) return participants;

            switch (_overflowBehavior)
            {
                case VjudgeOverflowBehavior.Truncate:
                    return participants.Take(MaxParticipantCount).ToList();
                case VjudgeOverflowBehavior.Sample:
                    return RandomSampler.Sample(participants, MaxParticipantCount).ToList();
                default:
                    throw new ArgumentOutOfRangeException(nameof(_overflowBehavior));
            }
        }

        private void ExportParticipant(ExcelWorksheet workSheet,
            int row,
            Participant participant,
            IDictionary<string, int> problemColumns)
        {
            workSheet.Cells[row, 1].Value = participant.Handle;

            // vjudge is picky about the excel it accepts - while filling unused
            // cells with spaces seems to be pointless, it makes the resulting XML
            // in the excel zip palatable for vjudge (if we don't make a space cell,
            // there will be no XML element for the cell, and I suspect vjudge doesn't
            // like that.
            var maxColumnId = problemColumns.Values.Max();
            for (var i = 2; i <= maxColumnId; i++) workSheet.Cells[row, i].Value = " ";

            var problemStatuses = participant.ProblemStatuses;
            if (problemStatuses == null)
            {
                // In COOK77 (and maybe some others), there are some participants with no ProblemStatuses
                // object (but a positive time penalty!). I don't really know what they are (maybe removed cheaters?),
                // because usually CodeChef doesn't list participants with no accepted solutions in the ranklist.
                // Here, we just assume that they are users with no submissions.
                Console.WriteLine($"Found user {participant.Handle} with no problem status dictionary, " +
                                  "assuming no problem solved.");
                problemStatuses = new Dictionary<string, ProblemStatus>();
            }

            foreach (var status in problemStatuses)
            {
                if (!problemColumns.ContainsKey(status.Key)) continue;
                workSheet.Cells[row, problemColumns[status.Key]].Value = CreateStatusCell(status.Value);
            }
        }

        private string CreateStatusCell(ProblemStatus status)
        {
            // vjudge format: "{penalty time} # {number of submissions including successful}"
            // they support others as well, but this seems to be the first suggestion.
            var timeWithPenalty = TimeSpan.Parse(status.Time);
            Debug.Assert(status.Penalty != null, "status.Penalty != null");
            var time = timeWithPenalty - status.Penalty.Value * _penaltyTime;
            return $"{(int) time.TotalMinutes} # {status.Penalty.Value + 1}";
        }
    }
}