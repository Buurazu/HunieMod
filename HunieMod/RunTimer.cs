using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HunieMod
{
    public class RunTimer
    {
        public static BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("RunTimer");
        public enum SplitColors
        {
            WHITE, BLUE, RED, GOLD
        }
        public const int GETLAID = 0, GETLAIDKYU = 1, ALLMAINGIRLS = 2, ALLPANTIES = 3, HUNDREDPERCENT = 4, INTRO = 5, NONE = 6, ANYCATEGORY = 7;
        public static string[] categories = new string[] { "Get Laid", "Get Laid + Kyu", "All Main Girls", "All Panties", "100%", "Intro%", "None", "Any Category" };
        public static int[] goals = new int[] { 1, 2, 8, 12, 100, 10000 };
        public static string[] difficulties = new string[] { "Any Difficulty", "Easy", "Normal", "Hard" };

        //white, blue, red, gold
        public static Color32[] outlineColors = new Color32[] { new Color32(98, 149, 252, 255), new Color(229, 36, 36, 255), new Color(240, 176, 18, 255) };
        //too dark
        //public static string[] darkColors = new string[] { "#ffffff", "#4bb7e8", "#e24c3c", "#ddaf4c" };
        //taken directly from the seed counts; too light
        //public static string[] colors = new string[] { "#ffffff", "#d6e9ff", "#ffcccc", "#ddaf4c" };
        public static string[] colors = new string[] { "dbdbdb", "98d5d7", "ffb2b2", "f1d983" };
        //public static Color[] unityColors = new Color[] { new Color(255, 255, 255), new Color(178, 214, 255), new Color(255, 178, 178), new Color(221, 175, 76) };

        public long runTimer;
        public int runFile;
        public string category;
        public int chosenCategory;
        public int chosenDifficulty;
        public int goal;
        public bool finishedRun;
        public bool switchedCategory;
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
                val += Mathf.Abs(time.Hours) + ":" + new DateTime(time.Duration().Ticks).ToString(@"mm\:ss\.f");
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
                TimeSpan s1 = ReadFile(target); TimeSpan s2 = ReadFile(target2);
                if (s1 != TimeSpan.Zero && s2 != TimeSpan.Zero)
                {
                    if (chop) val = convert(s1 + s2);
                    else val = (s1 + s2).ToString();
                }
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
                TimeSpan s1 = ReadFile(target); TimeSpan s2 = ReadFile(target2);
                if (s1 != TimeSpan.Zero && s2 != TimeSpan.Zero) val = convert(s1 + s2);
            }
            return val;
        }

        //adds contents of the file to the given list, if provided
        //returns the sum of all found timespans
        private static TimeSpan ReadFile(string target, List<TimeSpan> list = null)
        {
            TimeSpan sum = new TimeSpan();
            if (File.Exists(target))
            {
                string[] textFile = File.ReadAllLines(target);
                for (int j = 0; j < textFile.Length; j++)
                {
                    TimeSpan temp = TimeSpan.Parse(textFile[j]);
                    if (temp.Ticks > 0)
                    {
                        if (list != null) list.Add(temp);
                        sum += temp;
                    }
                    else
                    {
                        //a negative value was found in a saved file. this should be impossible but has happened before
                        if (list != null) list.Clear();
                        File.Delete(target);
                        return TimeSpan.Zero;
                    }
                }
            }
            return sum;
        }

        public static void ConvertOldSplits()
        {
            //skip Any Difficulty
            for (int d = 1; d < difficulties.Length; d++)
            {
                string category = "Unlock Venus" + " " + difficulties[d];
                string category2 = "All Main Girls" + " " + difficulties[d];
                //string target = "splits/data/" + category + ".txt";
                if (GetPB(category) != "N/A")
                {
                    string old2 = "splits/data/" + category + " Dates.txt";
                    string old3 = "splits/data/" + category + " Bonuses.txt";
                    string target2 = "splits/data/" + category2 + " Dates.txt";
                    string target3 = "splits/data/" + category2 + " Bonuses.txt";

                    File.Move(old2, target2);
                    File.Move(old3, target3);
                    // remove the last line of the dates file since it's unlocking venus
                    var textFile = File.ReadAllLines(target2);
                    File.WriteAllLines(target2, File.ReadAllLines(target2).Take(textFile.Length - 1).ToArray());

                    // do the same for the gold files
                    old2 = "splits/data/" + category + " Dates Golds.txt";
                    old3 = "splits/data/" + category + " Bonuses Golds.txt";
                    target2 = "splits/data/" + category2 + " Dates Golds.txt";
                    target3 = "splits/data/" + category2 + " Bonuses Golds.txt";

                    File.Move(old2, target2);
                    File.Move(old3, target3);
                    // remove the last line of the dates file since it's unlocking venus
                    textFile = File.ReadAllLines(target2);
                    File.WriteAllLines(target2, File.ReadAllLines(target2).Take(textFile.Length - 1).ToArray());
                }
            }
        }




        public RunTimer()
        {
            //no new file, so it's just practice
            runFile = -1;
            category = "";
            goal = -1;
            chosenCategory = -1;
            chosenDifficulty = -1;
            runTimer = DateTime.UtcNow.Ticks;
            finishedRun = false;
            switchedCategory = false;
            Logger.LogMessage("new RunTimer created");
        }
        public RunTimer(int newFile, int cat, int difficulty) : this()
        {
            //beginning a new run
            runFile = newFile;
            if (cat < categories.Length)
            {
                //default to Normal
                if (difficulty == 0) difficulty = 2;
                if (cat == RunTimer.NONE)
                {
                    category = ""; finishedRun = true; return;
                }
                else if (cat == RunTimer.ANYCATEGORY) cat = 0;
                category = categories[cat] + " " + difficulties[difficulty];
                goal = goals[cat];

                chosenCategory = cat;
                chosenDifficulty = difficulty;

                refresh();
            }
            else
            {
                Logger.LogMessage("invalid category, so no category loaded");
            }
            
        }

        public void refresh()
        {
            category = categories[chosenCategory] + " " + difficulties[chosenDifficulty];
            goal = goals[chosenCategory];

            Logger.LogMessage("category+difficulty chosen: " + category);

            comparisonDates.Clear(); comparisonBonuses.Clear();
            goldDates.Clear(); goldBonuses.Clear();

            //search for comparison date splits
            string target = "splits/data/" + category + " Dates.txt"; string target2 = "splits/data/" + category + " Bonuses.txt";
            ReadFile(target, comparisonDates); ReadFile(target2, comparisonBonuses);
            
            //search for gold splits
            target = "splits/data/" + category + " Dates Golds.txt"; target2 = "splits/data/" + category + " Bonuses Golds.txt";
            ReadFile(target, goldDates); ReadFile(target2, goldBonuses);

            /*if (tutorialTime != TimeSpan.Zero) {
                if (goldDates.Count == 0)
                    goldDates.Add(tutorialTime);
                else if (goldDates[0] > tutorialTime)
                    goldDates[0] = tutorialTime;
            }*/
            //merge our golds with the new category choice's golds
            int dateNum = 0, bonusNum = 0;
            
            for (int i = 0; i < splits.Count; i++)
            {
                TimeSpan s = splits[i];
                if (i > 0) s = s - splits[i-1];
                if (isBonus[i])
                {
                    if (goldBonuses.Count <= bonusNum)
                    {
                        goldBonuses.Add(s);
                    }
                    else if (goldBonuses[bonusNum] > s)
                    {
                        goldBonuses[bonusNum] = s;
                    }
                    bonusNum++;
                }
                else
                {
                    if (goldDates.Count <= dateNum)
                    {
                        goldDates.Add(s);
                    }
                    else if (goldDates[dateNum] > s)
                    {
                        goldDates[dateNum] = s;
                    }
                    dateNum++;
                }
            }

            /*foreach (TimeSpan s in goldDates) BasePatches.Logger.LogMessage(s);
            BasePatches.Logger.LogMessage("bonuses");
            foreach (TimeSpan s in goldBonuses) BasePatches.Logger.LogMessage(s);*/
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
            long tickDiff = DateTime.UtcNow.Ticks - runTimer;
            //I hope this code never runs
            if (tickDiff < 0)
            {
                reset(false);
                GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Timer somehow became negative; run quit! Please report this!");
                return false;
            }
            splits.Add(new TimeSpan(tickDiff));
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

            //only show split difference if we're on a category, and our difficulty matches (or we're on tutorial)
            if (category != "" && (splits.Count == 1 || chosenDifficulty == (int)GameManager.System.Player.settingsDifficulty + 1))
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
            else if (category == "" && finishedRun == false)
            {
                //reset the timer for each split if we aren't in a category or post-run
                runTimer = DateTime.UtcNow.Ticks;
            }

            splitText = val;
            //Logger.LogMessage(splitText + " " + goldText);
            return true;
        }

        //aka "save golds"
        public void reset(bool saveGolds = true)
        {
            //save golds on reset of a category
            if (category != "" && saveGolds && chosenDifficulty == (int)GameManager.System.Player.settingsDifficulty + 1)
            {
                //save date and bonus golds separately, but without copying all the code twice lol
                string[] targets = { "splits/data/" + category + " Dates Golds.txt", "splits/data/" + category + " Bonuses Golds.txt" };
                List<TimeSpan>[] golds = { goldDates, goldBonuses };
                for (int i = 0; i < targets.Length; i++)
                {
                    string target = targets[i]; List<TimeSpan> gold = golds[i];

                    if (File.Exists(target))
                    {
                        //merge the two golds lists
                        List<TimeSpan> prevGolds = new List<TimeSpan>(); ReadFile(target, prevGolds);
                        List<TimeSpan> newGolds = new List<TimeSpan>();

                        for (int j = 0; j < prevGolds.Count; j++)
                        {
                            //make sure our golds isn't too short to compare
                            if (gold.Count - 1 < j) { newGolds.Add(prevGolds[j]); }
                            else
                            {
                                if (gold[j] < prevGolds[j]) newGolds.Add(gold[j]);
                                else newGolds.Add(prevGolds[j]);
                            }
                        }
                        //make sure the file's golds isn't too short to compare
                        if (gold.Count > prevGolds.Count)
                        {
                            for (int j = prevGolds.Count; j < gold.Count; j++)
                            {
                                newGolds.Add(gold[j]);
                            }
                        }
                        File.WriteAllLines(target, spansToStrings(newGolds));
                    }
                    else
                    {
                        //create a new file with our current golds list
                        File.WriteAllLines(target, spansToStrings(gold));
                    }
                }

                Logger.LogMessage("writing PB Attempt.txt");
                File.WriteAllText("splits/" + category + " Last Attempt.txt", finalRunDisplay);
            }
            category = "";
            goal = -1;
            //runTimer = DateTime.UtcNow.Ticks;
            finishedRun = true;
        }

        //a run has finished; is it faster than our comparison?
        public void save()
        {
            if (category != "")
            {
                if (chosenDifficulty == (int)GameManager.System.Player.settingsDifficulty + 1)
                {
                    string target = "splits/data/" + category + " Dates.txt"; string target2 = "splits/data/" + category + " Bonuses.txt";
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
