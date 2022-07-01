

using System;
using System.IO;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Winner
{
    /// <summary>
    /// Used Powershell command tool :-  
    ///  1.  csc winner.cs
    ///  2.  .\winner.exe --in abc.txt --out xyz.txt
    ///
    ///  Uses ConsoleApp .net Core application; .net5
    ///
    /// </summary>
    class Winner
    {
        static string inputFilePath = string.Empty;
        static string outputFilePath = string.Empty;

        static int Main(string[] args)
        {
            FileStream outputFile = null;

            if (args.Length == 0)
            {                
                Console.Write("ERROR: Please enter input and output arguments.");                
                return 1;
            }
            else
            {                
                Console.Write("Arguments: ");

                foreach (var arg in args)
                {
                    Console.Write(arg + ", ");

                    if (arg.Contains("in"))
                    {                        
                        inputFilePath = $"{Environment.CurrentDirectory}\\{args[1].Trim()}";  
                    }    
                    else if (arg.Contains("out"))
                    {
                        outputFilePath = $"{Environment.CurrentDirectory}\\{args[3].Trim()}";
                        outputFile = File.Open(outputFilePath, FileMode.Create, FileAccess.ReadWrite);
                    }                
                }

                if (string.IsNullOrEmpty(inputFilePath) || string.IsNullOrEmpty(outputFilePath))
                {
                    Console.Write("ERROR: Please enter input and output arguments correctly.");
                    return 1;
                }

                string inputText = File.ReadAllText(inputFilePath);
                string[] playerListWithCards = inputText.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                List<(string playerName, int? highestScore, string playerCards)> highestScoreByPlayer = new List<(string playerName, int? highestScore, string playerCards)>();

                if (playerListWithCards.Length == 0)
                {
                    StringBuilder sbErrInput = new StringBuilder();
                    sbErrInput.AppendLine("ERROR: Empty Input file.");

                    byte[] err = new UTF8Encoding(true).GetBytes(sbErrInput.ToString());
                    outputFile.Write(err, 0, err.Length);
                    outputFile.Close();

                    return 1;
                }

                int? highestScore = 0;
                string highestPlayerName = string.Empty;
                string highestPlayerCards = string.Empty;

                foreach (string playerWithCards in playerListWithCards)
                {
                    if (playerWithCards.Contains(":"))
                    {
                        string[] playerNameAndCards = playerWithCards.Split(':');
                        if (playerNameAndCards.Length == 2)
                        {
                            string playerName = playerNameAndCards[0].ToString();
                            string playerCards = playerNameAndCards[1].ToString();
                            // calculate score
                            int? currentScore = calculateScore(playerCards);

                            if (currentScore >= highestScore)
                            {
                                highestScoreByPlayer.Add((playerName, currentScore, playerCards));                                

                                if (currentScore != highestScore)
                                {
                                    highestScoreByPlayer.Remove((highestPlayerName, highestScore, highestPlayerCards));
                                }

                                highestScore = currentScore;
                                highestPlayerName = playerName;
                                highestPlayerCards = playerCards;
                            }                            
                        }
                    }                      
                }

                //if tie, use Suite score to negotiate tie;
                List<(string playerName, int? highestScore, string playerCards)> highestScoreWithSuiteByPlayer = null;
                if (highestScoreByPlayer.Count > 0)
                {
                    int? highestScoreWithSuite = 0;
                    highestScoreWithSuiteByPlayer = new List<(string playerName, int? highestScore, string playerCards)>();

                    foreach ((string playerName, int? highestScore, string playerCards) highestScoredPlayer in highestScoreByPlayer)
                    {
                        int? currentScoreWithSuite = calculateSuiteScore(highestScoredPlayer.playerCards);
                        if (currentScoreWithSuite >= highestScoreWithSuite)
                        {
                            highestScoreWithSuiteByPlayer.Add((highestScoredPlayer.playerName, currentScoreWithSuite, highestScoredPlayer.playerCards));

                            if (currentScoreWithSuite != highestScoreWithSuite)
                            {
                                highestScoreWithSuiteByPlayer.Remove((highestScoredPlayer.playerName, highestScoredPlayer.highestScore, highestScoredPlayer.playerCards));
                            }

                            highestScoreWithSuite = currentScoreWithSuite;
                            highestPlayerName = highestScoredPlayer.playerName;
                            highestPlayerCards = highestScoredPlayer.playerCards;
                        }
                    }
                }
               
                if (highestScoreWithSuiteByPlayer != null)
                {
                    highestScoreByPlayer = new List<(string playerName, int? highestScore, string playerCards)>(); // renew this structure with SuiteScored as well
                   
                    foreach((string playerName, int? highestScore, string playerCards) highPlayer in highestScoreWithSuiteByPlayer)
                    {
                        highestScoreByPlayer.Add((highPlayer.playerName, highPlayer.highestScore, highPlayer.playerCards));
                    }

                }
                   
                StringBuilder sb = new StringBuilder();
             
                if (highestScoreByPlayer.Count > 1)
                {
                    int i = 0;
                    foreach ((string playerName, int? highestScore, string playerCards) highScore in highestScoreByPlayer)
                    {
                        if (i != highestScoreByPlayer.Count-1)
                            sb.Append(highScore.playerName + ",");
                        else
                            sb.Append(highScore.playerName);

                        i++;
                    }
                    sb.Append(":" + highestScoreByPlayer[0].highestScore);
                }
                else
                {
                    foreach ((string playerName, int? highestScore, string playerCards) highScore in highestScoreByPlayer)
                    {
                        sb.AppendLine(highScore.playerName + ":" + highScore.highestScore);
                    }
                }                
               
                byte[] info = new UTF8Encoding(true).GetBytes(sb.ToString());
                outputFile.Write(info, 0, info.Length);
                outputFile.Close();
                 
                return 0;
            }
        }
   
        static int? calculateScore(string cards)
        {
            int? score = null;
            string[] playerCards = cards.Split(',');

            if (playerCards.Length == 5) // 5 cards per player
            {
                score = 0;
                foreach (string card in playerCards)
                {                    
                    FaceValue result = 0;
                    string suite = card.Substring(card.Length - 1);  
                    string faceValueCard = card.TrimEnd(card[card.Length - 1]);

                    if (Enum.TryParse<FaceValue>(faceValueCard.ToString(), out result))
                    {
                        score += (int)(FaceValue)Enum.Parse(typeof(FaceValue), faceValueCard.ToString());
                    }
                }
            }

            return score;
        }

        static int? calculateSuiteScore(string cards)
        {
            int? score = null;
            string[] playerCards = cards.Split(',');

            if (playerCards.Length == 5) // 5 cards per player
            {
                score = 0;
                foreach (string card in playerCards)
                {
                    FaceValue result = 0;
                    string suite = card.Substring(card.Length - 1);  // card.TakeLast(1).FirstOrDefault();
                    string faceValueCard = card.TrimEnd(card[card.Length - 1]);

                    if (Enum.TryParse<FaceValue>(faceValueCard.ToString(), out result))
                    {
                        score += (int)(FaceValue)Enum.Parse(typeof(FaceValue), faceValueCard.ToString());
                        score += (int)(Suite)Enum.Parse(typeof(Suite), suite.ToString());
                    }
                }
            }

            return score;
        }
    }

     
    public enum Suite
    {
        [Description("Clubs")]
        C = 1,
        [Description("Diamonds")]
        D,
        [Description("Hearts")]
        H,
        [Description("Spades")]
        S        
    }

    public enum FaceValue
    {
        [Description("Ace")]
        A = 1,
        [Description("Two")]
        Two = 2,
        [Description("Three")]
        Three = 3,
        [Description("Four")]
        Four = 4,
        [Description("Five")]
        Five = 5,
        [Description("Six")]
        Six = 6,
        [Description("Seven")]
        Seven = 7,
        [Description("Eight")]
        Eight = 8,
        [Description("Nine")]
        Nine = 9,
        [Description("Ten")]
        Ten = 10,
        [Description("Jack")]
        J,
        [Description("Queen")]
        Q,
        [Description("King")]
        K
    }

}
