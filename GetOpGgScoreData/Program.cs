using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace GetOpGgScoreData
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter Summoner Name:");
            String summonerToSearch = Console.ReadLine();
            String  id = getSummonerIdFromSummonerName(summonerToSearch);
            List<LinkData> rankedLinkData = getAllRankedLinkData(id, "0");
            Console.WriteLine("Average Score " + getAverageOpGgScore(rankedLinkData, summonerToSearch));
            Console.ReadLine();
        }

        public static string getSummonerIdFromSummonerName(string summonerName)
        {
            var web = new HtmlAgilityPack.HtmlWeb();
            HtmlDocument doc;
            doc = web.Load("https://na.op.gg/summoner/champions/userName=" + summonerName);
            HtmlNode idButton = doc.DocumentNode.SelectSingleNode("//button[contains(@class, 'Button SemiRound Blue')]");
            string idJs = idButton.Attributes["onClick"].Value;
            Regex regex = new Regex("\'[0-9]+\'");
            String summonerId = regex.Match(idJs).Value;
            return summonerId.Replace("'", "");
        }

        public static double getAverageOpGgScore(List<LinkData> lld, string summonerName)
        {
            var web = new HtmlAgilityPack.HtmlWeb();
            double totalScore = 0.0;
            int total = 0;
            HtmlDocument doc;
            foreach(LinkData ld in lld)
            {
                doc = web.Load("https://na.op.gg/summoner/matches/ajax/detail/gameId="+ld.GameId+"&summonerId="+ld.SummonerId+"&gameTime="+ld.GameTime);
                HtmlNode scoreRow = doc.DocumentNode.SelectSingleNode("//tr[contains(@class, 'Row ')]//td[contains(@class, 'SummonerName Cell')]//a[contains(text(), '"+summonerName+"')]/../..");
                HtmlNode scoreCellText = scoreRow.SelectSingleNode("./td[contains(@class,'OPScore Cell')]//div[contains(@class, 'OPScore Text')]");
                if (scoreCellText != null)
                {
                    string score = scoreCellText.InnerText;
                    Console.WriteLine(score);
                    totalScore += Double.Parse(score);
                    total++;
                }
            }
            return totalScore/total;
        }

        public static List<LinkData> getAllRankedLinkData(string summonerId, string startInfo)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://na.op.gg/summoner/matches/ajax/averageAndList/startInfo="+startInfo+"&summonerId="+summonerId+"&type=soloranked");
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                dynamic json = JObject.Parse(reader.ReadToEnd());
                string html = json.html;
                string nextPageLink = json.lastInfo;
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                HtmlNodeCollection gameDataHtml = doc.DocumentNode.SelectNodes("//div[contains(@class, 'GameItem ')]");
                if (gameDataHtml == null)
                    return new List<LinkData>();
                else if (gameDataHtml.Count < 20)
                {
                    return getLinkData(gameDataHtml);
                }
                else
                {
                    List<LinkData> prevData = getAllRankedLinkData(summonerId, nextPageLink);
                    prevData.AddRange(getLinkData(gameDataHtml));
                    return prevData;
                }
            }
        }

        private static List<LinkData> getLinkData(HtmlNodeCollection nodes)
        {
            List<LinkData> linkDataList = new List<LinkData>(); 
            foreach(HtmlNode n in nodes)
            {
                LinkData ld = new LinkData();
                ld.SummonerId = n.Attributes["data-summoner-id"].Value;
                ld.GameTime = n.Attributes["data-game-time"].Value;
                ld.GameId = n.Attributes["data-game-id"].Value;
                linkDataList.Add(ld);
            }
            return linkDataList;
        }
    }

    class LinkData
    {
        private string gameId;
        private string summonerId;
        private string gameTime;

        public string GameId
        {
            get { return gameId; }
            set { gameId = value; } 
        }

        public string SummonerId
        {
            get { return summonerId; }
            set { summonerId = value; }
        }

        public string GameTime
        {
            get { return gameTime; }
            set { gameTime = value; }
        }
    }
}
