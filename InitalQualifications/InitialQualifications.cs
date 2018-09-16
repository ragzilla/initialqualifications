using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using UnityModManagerNet;
using TH20;

namespace InitalQualifications
{
    public class Settings : UnityModManager.ModSettings
    {
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }

    static class Main
    {
        public static bool enabled;
        public static Settings settings;
        public static UnityModManager.ModEntry.ModLogger Logger;

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
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
    }

    [HarmonyPatch(typeof(JobApplicantManager), "InitialisePools")]
    static class JobApplicantManager_InitialisePools_Patch
    {
        private static readonly Dictionary<StaffDefinition.Type, List<List<string>>> pools = new Dictionary<StaffDefinition.Type, List<List<string>>>() {
            {
                StaffDefinition.Type.Doctor, new List<List<string>>{
                new List<string> { "General Practice", "General Practice II", "General Practice III", "General Practice IV", "General Practice V" },
                new List<string> { "Radiology", "Diagnostics", "Diagnostics II", "Diagnostics III", "Bedside Manner" },
                new List<string> { "Treatment", "Treatment II", "Treatment III", "Treatment IV", "Treatment V" }
                }
            },
            {
                StaffDefinition.Type.Nurse, new List<List<string>>{
                new List<string> { "Diagnostics", "Diagnostics II", "Diagnostics III", "Diagnostics IV", "Diagnostics V" },
                new List<string> { "Treatment", "Treatment II", "Treatment III", "Treatment IV", "Treatment V" },
                new List<string> { "Ward Management", "Ward Management II", "Ward Management III", "Ward Management IV", "Ward Management V" }
                }
            },
            {
                StaffDefinition.Type.Assistant, new List<List<string>>{
                new List<string> { "Customer Service", "Customer Service II", "Customer Service III", "Customer Service IV", "Customer Service V" },
                new List<string> { "Customer Service", "Customer Service II", "Customer Service III", "Customer Service IV", "Customer Service V" },
                new List<string> { "Marketing", "Marketing II", "Marketing III", "Marketing IV", "Marketing V" }
                }
            },
            {
                StaffDefinition.Type.Janitor, new List<List<string>>{
                new List<string> { "Motivation", "Ghost Capture", "Maintenance", "Maintenance II", "Maintenance III" },
                new List<string> { "Motivation", "Mechanics", "Mechanics II", "Mechanics III", "Mechanics IV" },
                new List<string> { "Motivation", "Maintenance", "Maintenance II", "Maintenance III", "Maintenance IV" }
                }
            }
        };


        static void Postfix(JobApplicantManager __instance)
        {
            Dictionary<string, QualificationDefinition> defs = new Dictionary<string, QualificationDefinition>();
            foreach (KeyValuePair<QualificationDefinition, int> item in __instance.Qualifications.List)
            {
                defs.Add(item.Key.ToString(), item.Key);
            }
            foreach (KeyValuePair<StaffDefinition.Type, List<List<string>>> item in pools)
            {
                JobApplicantPool pool = __instance.GetJobApplicantPool(item.Key);
                int i = 0;
                foreach (JobApplicant applicant in pool.Applicants.OrderByDescending(d => d.Rank))
                {
                    bool fail = false;
                    var q = Traverse.Create(applicant).Property("Qualifications");
                    var target = q.GetValue<List<QualificationSlot>>();
                    target.Clear();
                    foreach (string job in item.Value[i])
                    {
                        if (!defs.ContainsKey(job))
                        {
                            Main.Logger.Error($"Unable to locate qualification {job} in current level");
                            fail = true;
                            break;
                        }
                        target.Add(new QualificationSlot(defs[job], true));
                    }
                    if (!fail) q.SetValue(target);
                    i++;
                }
            }

            return;
        }
    }

    [HarmonyPatch(typeof(JobApplicant), "AssignRandomQualifications")]
    class JobApplicant_AssignRandomQualifications_Patch
    {
        private static readonly Dictionary<string, List<string>> builds = new Dictionary<string, List<string>>()
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

        static void Postfix(JobApplicant __instance, WeightedList<QualificationDefinition> qualifications)
        {
            bool fail = false;
            WeightedList<string> buildlist = new WeightedList<string>();
            var q = Traverse.Create(__instance).Property("Qualifications");
            List<QualificationSlot> target = q.GetValue<List<QualificationSlot>>();
            target.Clear();

            Dictionary<string, QualificationDefinition> defs = new Dictionary<string, QualificationDefinition>();
            foreach (KeyValuePair<QualificationDefinition, int> item in qualifications.List)
            {
                defs.Add(item.Key.ToString(), item.Key);
            }

            foreach (KeyValuePair<string, int> item in statistics[__instance.Definition._type]) buildlist.Add(item.Key, item.Value);

            foreach (string job in builds[buildlist.Choose(null, RandomUtils.GlobalRandomInstance)])
            {
                if (!defs.ContainsKey(job))
                {
                    Main.Logger.Error($"Unable to locate qualification {job} in current level");
                    fail = true;
                    break;
                }
                target.Add(new QualificationSlot(defs[job], true));
            }
            if (!fail) q.SetValue(target);
        }
    }
}
