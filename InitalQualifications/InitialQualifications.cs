using System.Collections.Generic;
using System.Reflection;
using Harmony;
using UnityModManagerNet;
using UnityEngine;
using TH20;

namespace InitalQualifications
{
    public class Settings : UnityModManager.ModSettings
    {
        public int RespecInitialPool = 2;
        public int RespecFutureApplicants = 2;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }

    public class Qualifications
    {
        public Dictionary<string, QualificationDefinition> Definitions;

        public void Process(WeightedList<QualificationDefinition> input)
        {
            if (Definitions == null)
            {
                Definitions = new Dictionary<string, QualificationDefinition>();
                foreach (KeyValuePair<QualificationDefinition, int> item in input.List)
                    Definitions.Add(item.Key.ToString(), item.Key);
            }
        }
    }

    static class Main
    {
        public static bool enabled;
        public static Settings settings;
        public static Qualifications qualifications = new Qualifications();
        public static UnityModManager.ModEntry.ModLogger Logger;

        public static bool InInitialisePools = false;

        public static readonly Dictionary<string, List<string>> Builds = new Dictionary<string, List<string>>()
        {
            // Shared
            { "Treatment", new List<string> { "Treatment", "Treatment II", "Treatment III", "Treatment IV", "Treatment V" } },

            // Doctors
            { "General Practioner", new List<string> { "General Practice", "General Practice II", "General Practice III", "General Practice IV", "General Practice V" } },
            { "Radiologist",        new List<string> { "Radiology", "Diagnostics", "Diagnostics II", "Diagnostics III", "Bedside Manner" } },
            { "Geneticist",         new List<string> { "Genetics", "Diagnostics", "Treatment", "Diagnostics II", "Treatment II" } },
            { "Surgeon",            new List<string> { "Surgery", "Surgery II", "Surgery III", "Surgery IV", "Surgery V" } },
            { "Researcher",         new List<string> { "Research", "Research II", "Research III", "Research IV", "Research V" } },
            { "Psychiatrist",       new List<string> { "Psychiatry", "Psychiatry II", "Psychiatry III", "Psychiatry IV", "Psychiatry V" } },

            // Nurses
            { "Diagnostics",        new List<string> { "Diagnostics", "Diagnostics II", "Diagnostics III", "Diagnostics IV", "Diagnostics V" } },
            { "Ward Nurse",         new List<string> { "Ward Management", "Ward Management II", "Ward Management III", "Ward Management IV", "Ward Management V" } },
            { "Pharmacist",         new List<string> { "Pharmacy Management", "Treatment", "Treatment II", "Treatment III", "Treatment IV" } },
            { "Injection",          new List<string> { "Injection Administration", "Treatment", "Treatment II", "Treatment III", "Treatment IV" } },

            // Assistants
            { "Customer Service",   new List<string> { "Customer Service", "Customer Service II", "Customer Service III", "Customer Service IV", "Customer Service V" } },
            { "Marketer",           new List<string> { "Marketing", "Marketing II", "Marketing III", "Marketing IV", "Marketing V" } },

            // Janitors
            { "Ghost Mechanic",     new List<string> { "Ghost Capture", "Motivation", "Mechanics", "Mechanics II", "Mechanics III" } },
            { "Ghost Repair",       new List<string> { "Ghost Capture", "Motivation", "Maintenance", "Maintenance II", "Maintenance III" } },
            { "Mechanic",           new List<string> { "Motivation", "Mechanics", "Mechanics II", "Mechanics III", "Mechanics IV" } },
            { "Repair",             new List<string> { "Motivation", "Maintenance", "Maintenance II", "Maintenance III", "Maintenance IV" } },

            // Terminator
            { "", new List<string> { } }
        };

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            settings = Settings.Load<Settings>(modEntry);

            Logger = modEntry.Logger;

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;

            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            string[] selStrings = new string[] { "off", "student", "random", "max" };
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(" Respec initial applicant qualifications.");
            settings.RespecInitialPool = GUILayout.SelectionGrid(settings.RespecInitialPool, selStrings, 4);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(" Respec future applicant qualifications.");
            settings.RespecFutureApplicants = GUILayout.SelectionGrid(settings.RespecFutureApplicants, selStrings, 4);
            GUILayout.EndHorizontal();
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        public static bool ProcessJobApplicant(ref JobApplicant jobApplicant, string build)
        {
            if (!Builds.ContainsKey(build))
                return false;

            var q = Traverse.Create(jobApplicant).Property("Qualifications");
            var target = q.GetValue<List<QualificationSlot>>();
            target.Clear();
            int i = 0;

            foreach (string job in Builds[build])
            {
                if (i++ > jobApplicant.MaxQualifications) break;
                if (!qualifications.Definitions.ContainsKey(job))
                {
                    Logger.Error($"Unable to locate qualification {job} in current level.");
                    return false;
                }
                target.Add(new QualificationSlot(qualifications.Definitions[job], true));
            }
            q.SetValue(target);

            return true;
        }
    }

    [HarmonyPatch(typeof(JobApplicantManager), "InitialisePools")]
    static class JobApplicantManager_InitialisePools_Patch
    {
        private static readonly Dictionary<StaffDefinition.Type, List<string>> pools = new Dictionary<StaffDefinition.Type, List<string>>() {
            { StaffDefinition.Type.Doctor,    new List<string>{ "General Practioner", "Radiologist", "Treatment" } },
            { StaffDefinition.Type.Nurse,     new List<string>{ "Diagnostics",        "Treatment",   "Ward Nurse" } },
            { StaffDefinition.Type.Assistant, new List<string>{ "Customer Service",   "Marketer",    "Customer Service" } },
            { StaffDefinition.Type.Janitor,   new List<string>{ "Ghost Repair",       "Mechanic",    "Repair" } }
        };

        static void Prefix(JobApplicantManager __instance)
        {
            if (!Main.enabled || Main.settings.RespecInitialPool == 0)
                return;
            Main.InInitialisePools = true;
            Main.qualifications.Process(__instance.Qualifications);
        }

        static void Postfix(JobApplicantManager __instance)
        {
            if (!Main.enabled || Main.settings.RespecInitialPool == 0)
                return;
            foreach (KeyValuePair<StaffDefinition.Type, List<string>> item in pools)
            {
                JobApplicantPool pool = __instance.GetJobApplicantPool(item.Key);
                pool.Applicants.Sort((a, b) => b.Rank - a.Rank);
                for (int i = 0; i < pool.Applicants.Count; i++)
                {
                    JobApplicant temp = pool.Applicants[i];
                    Main.ProcessJobApplicant(ref temp, pools[item.Key][i]);
                    pool.Applicants[i] = temp;
                }
            }
            Main.InInitialisePools = false;
        }
    }

    [HarmonyPatch(typeof(JobApplicant), "AssignRandomQualifications")]
    class JobApplicant_AssignRandomQualifications_Patch
    {
        private static readonly Dictionary<StaffDefinition.Type, Dictionary<string, int>> statistics = new Dictionary<StaffDefinition.Type, Dictionary<string, int>>()
        {
            {
                StaffDefinition.Type.Doctor, new Dictionary<string, int>
                {
                    { "General Practioner", 100 },
                    { "Radiologist", 50 },
                    { "Geneticist", 50 },
                    { "Surgeon", 50 },
                    { "Researcher", 25 },
                    { "Psychiatrist", 50 },
                    { "Treatment", 100 }
                }
            },
            {
                StaffDefinition.Type.Nurse, new Dictionary<string, int>
                {
                    { "Diagnostics", 200 },
                    { "Ward Nurse", 100 },
                    { "Pharmacist", 100 },
                    { "Injection", 100 },
                    { "Treatment", 200 }
                }
            },
            {
                StaffDefinition.Type.Assistant, new Dictionary<string, int>
                {
                    { "Customer Service", 200 },
                    { "Marketer", 100 },
                }
            },
            {
                StaffDefinition.Type.Janitor, new Dictionary<string, int>
                {
                    { "Ghost Mechanic", 50 },
                    { "Ghost Repair", 50 },
                    { "Mechanic", 100 },
                    { "Repair", 100 }
                }
            }
        };

        static void Postfix(JobApplicant __instance, WeightedList<QualificationDefinition> qualifications, Metagame metagame, Level level, int chanceOfEmptyTrainingSlot)
        {
            if (!Main.enabled || Main.InInitialisePools || Main.settings.RespecFutureApplicants == 0)
                return;
            Main.qualifications.Process(qualifications);

            WeightedList<string> buildlist = new WeightedList<string>();
            foreach (KeyValuePair<string, int> item in statistics[__instance.Definition._type]) buildlist.Add(item.Key, item.Value);

            Main.ProcessJobApplicant(ref __instance, buildlist.Choose(null, RandomUtils.GlobalRandomInstance));
        }
    }
}
