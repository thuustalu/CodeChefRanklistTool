using System.Collections.Generic;
using System.Text.Json.Serialization;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace CodeChefRanklistTool
{
    public class RanklistResponse
    {
        [JsonPropertyName("contest_info")] public Contest Contest { get; set; }

        [JsonPropertyName("problems")] public List<Problem> Problems { get; set; } = new List<Problem>();

        [JsonPropertyName("list")] public List<Participant> Participants { get; set; } = new List<Participant>();

        [JsonPropertyName("totalItems")] public int? TotalItems { get; set; }

        [JsonPropertyName("availablePages")] public int? AvailablePages { get; set; }
    }

    public class Contest
    {
        [JsonPropertyName("contest_code")] public string ContestCode { get; set; }

        [JsonPropertyName("ranking_type")] public string RankingType { get; set; }

        [JsonPropertyName("is_team_based")] public bool? IsTeamBased { get; set; }

        [JsonPropertyName("is_ranklist_frozen")]
        public bool? IsRanklistFrozen { get; set; }

        [JsonPropertyName("time")] public ContestTimeSpan ContestTimeSpan { get; set; }

        [JsonPropertyName("unscored_problems")]
        public List<string> UnscoredProblems { get; set; } = new List<string>();

        public string DisplayName { get; set; }
    }

    public class ContestTimeSpan
    {
        [JsonPropertyName("start")] public int? Start { get; set; }

        [JsonPropertyName("end")] public int? End { get; set; }

        [JsonPropertyName("current")] public int? Current { get; set; }

        [JsonPropertyName("freezing")] public int? Freezing { get; set; }
    }

    public class Problem
    {
        [JsonPropertyName("code")] public string Code { get; set; }

        [JsonPropertyName("name")] public string DisplayName { get; set; }
    }

    public class Participant
    {
        [JsonPropertyName("user_handle")] public string Handle { get; set; }

        [JsonPropertyName("score")] public int? Score { get; set; }

        [JsonPropertyName("total_time")] public string TotalTime { get; set; }

        [JsonPropertyName("penalty")] public int? Penalty { get; set; }

        [JsonPropertyName("problems_status")]
        public Dictionary<string, ProblemStatus> ProblemStatuses { get; set; } =
            new Dictionary<string, ProblemStatus>();
    }

    public class ProblemStatus
    {
        [JsonPropertyName("score")] public int? Score { get; set; }

        [JsonPropertyName("time")] public string Time { get; set; }

        [JsonPropertyName("penalty")] public int? Penalty { get; set; }
    }
}