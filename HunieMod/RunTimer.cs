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
        public List<bool> isBonus = new List<bool>();
        public List<TimeSpan> comparisonDates = new List<TimeSpan>();
        public List<TimeSpan> comparisonBonuses = new List<TimeSpan>();
        public List<TimeSpan> goldDates = new List<TimeSpan>();
        public List<TimeSpan> goldBonuses = new List<TimeSpan>();
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
        public static string GetPB(string category, bool chop = true)
        {
            string val = "N/A";
            string target = "splits/data/" + category + " Dates.txt";
            string target2 = "splits/data/" + category + " Bonuses.txt";
            if (File.Exists(target) && File.Exists(target2))
            {
                string[] textFile = File.ReadAllLines(target); string[] textFile2 = File.ReadAllLines(target2);
                TimeSpan s = new TimeSpan();
                foreach (string line in textFile)
                {
                    s += TimeSpan.Parse(line);
                }
                foreach (string line in textFile2)
                {
                    s += TimeSpan.Parse(line);
                }
                if (chop) val = convert(s);
                else val = s.ToString();
            }
            return val;
        }
        public static string GetPB(int cat, int difficulty)
        {
            return GetPB(categories[cat] + " " + difficulties[difficulty]);
        }
        public static string GetGolds(int cat, int difficulty)
        {
            string val = "N/A";
            if (GetPB(cat, difficulty) == "N/A") return val;
            string target = "splits/data/" + categories[cat] + " " + difficulties[difficulty] + " Dates Golds.txt";
            string target2 = "splits/data/" + categories[cat] + " " + difficulties[difficulty] + " Bonuses Golds.txt";
            if (File.Exists(target) && File.Exists(target2))
            {
                string[] textFile = File.ReadAllLines(target); string[] textFile2 = File.ReadAllLines(target2);
                TimeSpan s = new TimeSpan();
                foreach (string line in textFile)
                {
                    s += TimeSpan.Parse(line);
                }
                foreach (string line in textFile2)
                {
                    s += TimeSpan.Parse(line);
                }
                val = convert(s);
            }
            return val;
        }

        public static void ConvertOldSplits()
        {
            for (int c = 0; c < categories.Length; c++)
            {
                //skip Any Difficulty
                for (int d = 1; d < difficulties.Length; d++)
                {
                    string category = categories[c] + " " + difficulties[d];
                    string target = "splits/data/" + category + ".txt";
                    if (File.Exists(target))
                    {
                        string target2 = "splits/data/" + category + " Dates.txt";
                        string target3 = "splits/data/" + category + " Bonuses.txt";
                        List<TimeSpan> dateSplits = new List<TimeSpan>();
                        List<TimeSpan> bonusSplits = new List<TimeSpan>();
                        string[] textFile = File.ReadAllLines(target);
                        for (int j = 0; j < textFile.Length; j++)
                        {
                            TimeSpan t = TimeSpan.Parse(textFile[j]);
                            if (j > 0) t = t - TimeSpan.Parse(textFile[j - 1]);
                            if (t.TotalMinutes > 1)
                            {
                                dateSplits.Add(t);
                            }
                            else bonusSplits.Add(t);
                        }
                        File.WriteAllLines(target2, spansToStrings(dateSplits));
                        File.WriteAllLines(target3, spansToStrings(bonusSplits));
                        File.Delete(target);
                    }
                    target = "splits/data/" + category + " Golds.txt";
                    if (File.Exists(target))
                    {
                        string target2 = "splits/data/" + category + " Dates Golds.txt";
                        string target3 = "splits/data/" + category + " Bonuses Golds.txt";
                        List<TimeSpan> dateSplits = new List<TimeSpan>();
                        List<TimeSpan> bonusSplits = new List<TimeSpan>();
                        string[] textFile = File.ReadAllLines(target);
                        for (int j = 0; j < textFile.Length; j++)
                        {
                            TimeSpan t = TimeSpan.Parse(textFile[j]);
                            if (t.TotalMinutes > 1)
                            {
                                dateSplits.Add(t);
                            }
                            else bonusSplits.Add(t);
                        }
                        File.WriteAllLines(target2, spansToStrings(dateSplits));
                        File.WriteAllLines(target3, spansToStrings(bonusSplits));
                        File.Delete(target);
                    }
                }
            }
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
            comparisonDates.Clear(); comparisonBonuses.Clear();
            goldDates.Clear(); goldBonuses.Clear();
            //search for comparison date splits
            string target = "splits/data/" + category + " Dates.txt"; string target2 = "splits/data/" + category + " Bonuses.txt";
            if (File.Exists(target))
            {
                string[] textFile = File.ReadAllLines(target);
                for (int j = 0; j < textFile.Length; j++)
                {
                    comparisonDates.Add(TimeSpan.Parse(textFile[j]));
                }
            }
            if (File.Exists(target2))
            {
                string[] textFile2 = File.ReadAllLines(target2);
                for (int j = 0; j < textFile2.Length; j++)
                {
                    comparisonBonuses.Add(TimeSpan.Parse(textFile2[j]));
                }
            }
            //search for gold splits
            target = "splits/data/" + category + " Dates Golds.txt"; target2 = "splits/data/" + category + " Bonuses Golds.txt";
            if (File.Exists(target))
            {
                string[] textFile = File.ReadAllLines(target);
                for (int j = 0; j < textFile.Length; j++)
                {
                    goldDates.Add(TimeSpan.Parse(textFile[j]));
                }
            }
            if (File.Exists(target2))
            {
                string[] textFile2 = File.ReadAllLines(target2);
                for (int j = 0; j < textFile2.Length; j++)
                {
                    goldBonuses.Add(TimeSpan.Parse(textFile2[j]));
                }
            }
        }

        public TimeSpan GetTimeAt(int numDates, int numBonuses)
        {
            TimeSpan s = new TimeSpan();
            for (int i = 0; i < numDates; i++)
                s += comparisonDates[i];
            for (int i = 0; i < numBonuses; i++)
                s += comparisonBonuses[i];
            return s;
        }

        public bool split(bool bonus = false)
        {
            splits.Add(runTimer.Elapsed);
            isBonus.Add(bonus);
            splitColor = SplitColors.WHITE; prevColor = SplitColors.WHITE; goldColor = SplitColors.WHITE;
            splitText = ""; prevText = ""; goldText = "";

            TimeSpan s = splits[splits.Count - 1];
            string val = convert(s);

            int numDates = 0, numBonuses = 0;
            foreach (bool b in isBonus)
            {
                if (b) numBonuses++; else numDates++;
            }

            if (category != "")
            {
                //create the affection meter replacement text
                //time [+/-]
                if (comparisonDates.Count >= numDates && comparisonBonuses.Count >= numBonuses)
                {
                    TimeSpan elapsedC = GetTimeAt(numDates, numBonuses);
                    val += " [";
                    TimeSpan diff = s - elapsedC;
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
                        TimeSpan prevElapsedC;
                        if (bonus) prevElapsedC = GetTimeAt(numDates, numBonuses - 1);
                        else prevElapsedC = GetTimeAt(numDates - 1, numBonuses);
                        diff2 = s2 - prevElapsedC;
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
                if (goldDates.Count >= numDates && goldBonuses.Count >= numBonuses)
                {
                    //get segment length
                    if (splits.Count > 1) s = s - splits[splits.Count - 2];
                    TimeSpan diff;
                    if (bonus) diff = s - goldBonuses[numBonuses - 1];
                    else diff = s - goldDates[numDates - 1];
                    if (diff.TotalSeconds < 0)
                    {
                        //new gold
                        goldText += "-";
                        splitColor = SplitColors.GOLD;
                        goldColor = SplitColors.GOLD;
                        if (bonus) goldBonuses[numBonuses - 1] = s;
                        else goldDates[numDates - 1] = s;
                    }
                    else
                        goldText += "+";
                    goldText += convert(diff);

                }
                //no gold to compare with, or no category defined
                else
                {
                    if (splits.Count > 1) s = s - splits[splits.Count - 2];
                    if (bonus) goldBonuses.Add(s);
                    else goldDates.Add(s);
                }
            }

            splitText = val;
            //Logger.LogMessage(splitText + " " + goldText);
            return true;
        }

        //aka "save golds"
        public void reset(bool saveGolds = true)
        {
            //save golds on reset of a category
            if (category != "" && saveGolds)
            {
                string target = "splits/data/" + category + " Dates Golds.txt"; string target2 = "splits/data/" + category + " Bonuses Golds.txt";
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
                        if (goldDates.Count - 1 < j) { newGolds.Add(prevGolds[j]); }
                        else
                        {
                            if (goldDates[j] < prevGolds[j]) newGolds.Add(goldDates[j]);
                            else newGolds.Add(prevGolds[j]);
                        }
                    }
                    //make sure the file's golds isn't too short to compare
                    if (goldDates.Count > prevGolds.Count)
                    {
                        for (int j = prevGolds.Count; j < goldDates.Count; j++)
                        {
                            newGolds.Add(goldDates[j]);
                        }
                    }
                    File.WriteAllLines(target, spansToStrings(newGolds));
                }
                else
                {
                    //create a new file with our current golds list
                    string[] goldsString = new string[goldDates.Count];
                    for (int i = 0; i < goldDates.Count; i++)
                    {
                        goldsString[i] = goldDates[i].ToString();
                    }
                    File.WriteAllLines(target, spansToStrings(goldDates));
                }
                //the same code again but for goldBonuses. zzz
                if (File.Exists(target2))
                {
                    //merge the two golds lists
                    string[] textFile = File.ReadAllLines(target2);
                    List<TimeSpan> prevGolds = new List<TimeSpan>();
                    List<TimeSpan> newGolds = new List<TimeSpan>();
                    for (int j = 0; j < textFile.Length; j++)
                    {
                        prevGolds.Add(TimeSpan.Parse(textFile[j]));
                    }
                    for (int j = 0; j < prevGolds.Count; j++)
                    {
                        //make sure our golds isn't too short to compare
                        if (goldBonuses.Count - 1 < j) { newGolds.Add(prevGolds[j]); }
                        else
                        {
                            if (goldBonuses[j] < prevGolds[j]) newGolds.Add(goldBonuses[j]);
                            else newGolds.Add(prevGolds[j]);
                        }
                    }
                    //make sure the file's golds isn't too short to compare
                    if (goldBonuses.Count > prevGolds.Count)
                    {
                        for (int j = prevGolds.Count; j < goldBonuses.Count; j++)
                        {
                            newGolds.Add(goldBonuses[j]);
                        }
                    }
                    File.WriteAllLines(target2, spansToStrings(newGolds));
                }
                else
                {
                    //create a new file with our current golds list
                    string[] goldsString = new string[goldBonuses.Count];
                    for (int i = 0; i < goldBonuses.Count; i++)
                    {
                        goldsString[i] = goldBonuses[i].ToString();
                    }
                    File.WriteAllLines(target2, spansToStrings(goldBonuses));
                }
                Logger.LogMessage("writing PB Attempt.txt");
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
                string target = "splits/data/" + category + " Dates.txt"; string target2 = "splits/data/" + category + " Bonuses.txt";
                //due to the Tutorial, even 48 shoes would have a bonus file
                if (File.Exists(target) && File.Exists(target2))
                {
                    //saved comparison is longer than our new one
                    if (TimeSpan.Parse(GetPB(category, false)) > splits[splits.Count - 1])
                    {
                        File.WriteAllLines(target, splitsToStrings(false));
                        File.WriteAllLines(target2, splitsToStrings(true));
                        File.WriteAllText("splits/" + category + " PB.txt", finalRunDisplay);
                    }
                }
                //no PB file, so make one
                else
                {
                    File.WriteAllLines(target, splitsToStrings(false));
                    File.WriteAllLines(target2, splitsToStrings(true));
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

        private static string[] spansToStrings(List<TimeSpan> list)
        {
            string[] array = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                array[i] = list[i].ToString();
            }
            return array;
        }

        private string[] splitsToStrings(bool countingBonuses)
        {
            int numDates = 0, numBonuses = 0;
            foreach (bool b in isBonus)
            {
                if (b) numBonuses++; else numDates++;
            }
            string[] array;
            if (countingBonuses) array = new string[numBonuses];
            else array = new string[numDates];
            int counter = 0;
            for (int i = 0; i < splits.Count; i++)
            {
                TimeSpan s = splits[i];
                if (i > 0) s = s - splits[i - 1];
                if (countingBonuses) Logger.LogMessage(splits[i].ToString());
                if ((isBonus[i] && countingBonuses) || (!isBonus[i] && !countingBonuses))
                {
                    array[counter] = s.ToString();
                    counter++;
                }
            }
            return array;
        }
    }
}
