# CodeChefRanklistTool

This is a tool to download ranklists of CodeChef contests. I used this to create virtual 
contests ("replays") of CodeChef contests in vjudge.

The project is written targeting .NET Core 3.1; it should be possible to run it on all
platforms.

**Important note:** Although I provided code to download contests from CodeChef, if you want to use a CodeChef
ranklist for something, please use the raw output provided in the data/ folder instead
of redownloading it. Let's try to not bother CodeChef with too many requests.

#### Example usage

To fetch a single contest from CodeChef and save it in vjudge format:

```dotnet CodeChefRanklistTool.dll --fromCode COOK125A```

To fetch a list of contests from CodeChef and save only the raw output from CodeChef 
(in JSON format, more or less the same format as returned from CodeChef):

```dotnet CodeChefRanklistTool.dll --fromList contests.txt --outputFormat Json```

If you are exporting into vjudge format (Excel sheet and a text file with contest info and problem
order), you can specify the penalty time in minutes (it varies between contests!) and behavior
if vjudge's limit on the number of contestants is exceeded.

```dotnet CodeChefRanklistTool.dll --fromCode COOK50 --onOverflow Sample --penaltyTime 20```

```dotnet CodeChefRanklistTool.dll --fromCode COOK115A --onOverflow Truncate --penaltyTime 10```
