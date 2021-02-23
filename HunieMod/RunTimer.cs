using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

namespace HunieMod
{
    public class RunTimer
    {
        public static BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("RunTimer");
        public enum SplitColors
        {
            WHITE, BLUE, RED, GOLD
        }
        public static string[] categories = new string[] { "Get Laid", "Get Laid + Kyu", "Unlock Venus", "All Panties", "100%" };
        public static int[] goals = new int[] { 1, 2, 69, 12, 100 };
        public static string[] difficulties = new string[] { "Any Difficulty", "Easy", "Normal", "Hard" };

        //white, blue, red, gold
        public static Color32[] outlineColors = new Color32[] { new Color32(98, 149, 252, 255), new Color(229, 36, 36, 255), new Color(240, 176, 18, 255) };
        //too dark
        //public static string[] darkColors = new string[] { "#ffffff", "#4bb7e8", "#e24c3c", "#ddaf4c" };
        //taken directly from the seed counts; too light
        //public static string[] colors = new string[] { "#ffffff", "#d6e9ff", "#ffcccc", "#ddaf4c" };
        public static string[] colors = new string[] { "dbdbdb", "98d5d7", "ffb2b2", "f1d983" };
        //public static Color[] unityColors = new Color[] { new Color(255, 255, 255), new Color(178, 214, 255), new Color(255, 178, 178), new Color(221, 175, 76) };

        public Stopwatch runTimer;
        public int runFile;
        public string category;
        public int goal;
        public List<TimeSpan> splits = new List<TimeSpan>();
        public List<TimeSpan> comparison = new List<TimeSpan>();
        public List<TimeSpan> golds = new List<TimeSpan>();
        public string splitText;
        public string prevText;
        public string goldText;
        public SplitColors splitColor;
        public SplitColors prevColor;
        public SplitColors goldColor;

        public string finalRunDisplay = "";

        //convert a timespan to the proper format string
        public static string convert(TimeSpan time)
        {
            string val = "";
            //the older .net version doesn't have timespan.tostring options but this works maybe?
            //datetime likes to start at 12 hours but is fine for quick minutes/seconds displaying
            if (time.Hours != 0)
                val += Mathf.Abs(time.Hours) + new DateTime(time.Duration().Ticks).ToString(@"mm\:ss\.f");
            else if (time.Minutes != 0)
                val += new DateTime(time.Duration().Ticks).ToString(@"m\:ss\.f");
            else
                val += new DateTime(time.Duration().Ticks).ToString(@"s\.f");
            return val;
        }
        public static string GetAll(int cat, int difficulty)
        {
            if (cat >= categories.Length || difficulty >= difficulties.Length) return "N/A";
            string val = categories[cat] + " " + difficulties[difficulty] + "\nPB: " + GetPB(cat, difficulty) + "\nSoB: " + GetGolds(cat, difficulty);
            return val;
        }
        public static string GetPB(int cat, int difficulty)
        {
            string val = "N/A";
            string target = "splits/data/" + categories[cat] + " " + difficulties[difficulty] + ".txt";
            if (File.Exists(target))
            {
                string[] textFile = File.ReadAllLines(target);
                //saved comparison is longer than our new one
                TimeSpan s = TimeSpan.Parse(textFile[textFile.Length - 1]);
                val = convert(s);
            }
            return val;
        }
        public static string GetGolds(int cat, int difficulty)
        {
            string val = "N/A";
            if (GetPB(cat, difficulty) == "N/A") return val;
            string target = "splits/data/" + categories[cat] + " " + difficulties[difficulty] + " Golds.txt";
            if (File.Exists(target))
            {
                string[] textFile = File.ReadAllLines(target);
                TimeSpan s = new TimeSpan();
                foreach (string line in textFile)
                {
                    s += TimeSpan.Parse(line);
                }
                val = convert(s);
            }
            return val;
        }

        public RunTimer()
        {
            //no new file, so it's just practice
            runFile = -1;
            category = "";
            goal = -1;
            runTimer = new Stopwatch();
            runTimer.Start();
        }
        public RunTimer(int newFile, int cat, int difficulty) : this()
        {
            //beginning a new run
            runFile = newFile;
            if (cat < categories.Length)
            {
                //default to Normal
                if (difficulty == 0) difficulty = 2;
                category = categories[cat] + " " + difficulties[difficulty];
                goal = goals[cat];

                refresh();
            }
            else
            {
                Logger.LogMessage("invalid category, so no category loaded");
            }
        }

        public void refresh()
        {
            Logger.LogMessage("run chosen: " + category);
            comparison.Clear(); golds.Clear();
            //search for comparison splits
            string target = "splits/data/" + category + ".txt";
            if (File.Exists(target))
            {
                string[] textFile = File.ReadAllLines(target);
                for (int j = 0; j < textFile.Length; j++)
                {
                    comparison.Add(TimeSpan.Parse(textFile[j]));
                }
            }
            //search for gold splits
            target = "splits/data/" + category + " Golds.txt";
            if (File.Exists(target))
            {
                string[] textFile = File.ReadAllLines(target);
                for (int j = 0; j < textFile.Length; j++)
                {
                    golds.Add(TimeSpan.Parse(textFile[j]));
                }
            }
        }

        public bool split()
        {
            splits.Add(runTimer.Elapsed);
            //Logger.LogMessage(runTimer.Elapsed.ToString());
            splitColor = SplitColors.WHITE; prevColor = SplitColors.WHITE; goldColor = SplitColors.WHITE;
            splitText = ""; prevText = ""; goldText = "";

            TimeSpan s = splits[splits.Count - 1];
            string val = convert(s);

            if (category != "")
            {
                //create the affection meter replacement text
                //time [+/-]
                if (comparison.Count >= splits.Count)
                {
                    val += " [";
                    TimeSpan diff = s - comparison[splits.Count - 1];
                    if (diff.TotalSeconds > 0)
                    {
                        val += "+";
                        splitColor = SplitColors.RED;
                    }
                    else
                    {
                        val += "-";
                        splitColor = SplitColors.BLUE;
                    }

                    val += convert(diff) + "]";

                    //create the this split text, which is just this split's diff minus the last split's diff
                    TimeSpan diff2;
                    if (splits.Count != 1)
                    {
                        TimeSpan s2 = splits[splits.Count - 2];
                        diff2 = s2 - comparison[splits.Count - 2];
                        diff2 = diff - diff2;
                    }
                    else
                    {
                        diff2 = diff;
                    }
                    if (diff2.TotalSeconds > 0)
                    {
                        prevText += "+";
                        prevColor = SplitColors.RED;
                    }
                    else
                    {
                        prevText += "-";
                        prevColor = SplitColors.BLUE;
                    }
                    prevText += convert(diff2);
                }

                //create the gold diff text
                if (golds.Count >= splits.Count)
                {
                    //get segment length
                    if (splits.Count > 1) s = s - splits[splits.Count - 2];
                    TimeSpan diff = s - golds[splits.Count - 1];
                    if (diff.TotalSeconds < 0)
                    {
                        //new gold
                        goldText += "-";
                        splitColor = SplitColors.GOLD;
                        goldColor = SplitColors.GOLD;
                        golds[splits.Count - 1] = s;
                    }
                    else
                        goldText += "+";
                    goldText += convert(diff);

                }
                //no gold to compare with, or no category defined
                else
                {
                    if (splits.Count > 1) s = s - splits[splits.Count - 2];
                    golds.Add(s);
                }
            }

            splitText = val;
            Logger.LogMessage(splitText + " " + goldText);
            return true;
        }

        //aka "save golds"
        public void reset(bool saveGolds = true)
        {
            //save golds on reset of a category
            if (category != "" && saveGolds)
            {
                string target = "splits/data/" + category + " Golds.txt";
                if (File.Exists(target))
                {
                    //merge the two golds lists
                    string[] textFile = File.ReadAllLines(target);
                    List<TimeSpan> prevGolds = new List<TimeSpan>();
                    List<TimeSpan> newGolds = new List<TimeSpan>();
                    for (int j = 0; j < textFile.Length; j++)
                    {
                        prevGolds.Add(TimeSpan.Parse(textFile[j]));
                    }
                    for (int j = 0; j < prevGolds.Count; j++)
                    {
                        //make sure our golds isn't too short to compare
                        if (golds.Count - 1 < j) { newGolds.Add(prevGolds[j]); }
                        else
                        {
                            if (golds[j] < prevGolds[j]) newGolds.Add(golds[j]);
                            else newGolds.Add(prevGolds[j]);
                        }
                    }
                    //make sure the file's golds isn't too short to compare
                    if (golds.Count > prevGolds.Count)
                    {
                        for (int j = prevGolds.Count; j < golds.Count; j++)
                        {
                            newGolds.Add(golds[j]);
                        }
                    }
                    File.WriteAllLines(target, spansToStrings(newGolds));
                }
                else
                {
                    //create a new file with our current golds list
                    string[] goldsString = new string[golds.Count];
                    for (int i = 0; i < golds.Count; i++)
                    {
                        goldsString[i] = new DateTime(golds[i].Ticks).ToString(@"h\:mm\:ss\.F");
                        //goldsString[i] = golds[i].ToString("g");
                    }
                    File.WriteAllLines(target, spansToStrings(golds));
                }
                //Logger.LogMessage("writing PB Attempt.txt");
                File.WriteAllText("splits/" + category + " Last Attempt.txt", finalRunDisplay);
            }
            category = "";
            goal = -1;
        }

        //a run has finished; is it faster than our comparison?
        public void save()
        {
            if (category != "")
            {
                string target = "splits/data/" + category + ".txt";
                if (File.Exists(target))
                {
                    string[] textFile = File.ReadAllLines(target);
                    //saved comparison is longer than our new one?
                    if (TimeSpan.Parse(textFile[textFile.Length - 1]) > splits[splits.Count - 1])
                    {
                        File.WriteAllLines(target, spansToStrings(splits));
                        File.WriteAllText("splits/" + category + " PB.txt", finalRunDisplay);
                    }
                }
                else
                {
                    File.WriteAllLines(target, spansToStrings(splits));
                    File.WriteAllText("splits/" + category + " PB.txt", finalRunDisplay);
                }
                //run is over, so we're no longer on a category
                reset();
            }
        }

        public void push(string s)
        {
            finalRunDisplay += s;
            //Logger.LogMessage(finalRunDisplay);
        }

        private string[] spansToStrings(List<TimeSpan> list)
        {
            string[] array = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                array[i] = list[i].ToString();
                //array[i] = list[i].ToString("g");
            }
            return array;
        }
    }
}
