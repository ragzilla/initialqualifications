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
                    Definitions.Add(item.Key.NameLocalised.Term, item.Key);
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
            { "Treatment",          new List<string> { "Qualification/Doctor_Treatment_1_Name",       "Qualification/Doctor_Treatment_2_Name",       "Qualification/Doctor_Treatment_3_Name",       "Qualification/Doctor_Treatment_4_Name",       "Qualification/Doctor_Treatment_5_Name"         } },

            // Doctors
            { "General Practioner", new List<string> { "Qualification/Doctor_GeneralPractice_1_Name", "Qualification/Doctor_GeneralPractice_2_Name", "Qualification/Doctor_GeneralPractice_3_Name", "Qualification/Doctor_GeneralPractice_4_Name", "Qualification/Doctor_GeneralPractice_5_Name"   } },
            { "Radiologist",        new List<string> { "Qualification/Doctor_Radiology_1_Name",       "Qualification/Doctor_Diagnosis_1_Name",       "Qualification/Doctor_Diagnosis_2_Name",       "Qualification/Doctor_Diagnosis_3_Name",       "Qualification/General_PatientHappiness_1_Name" } },
            { "Geneticist",         new List<string> { "Qualification/Doctor_Genetics_1_Name",        "Qualification/Doctor_Diagnosis_1_Name",       "Qualification/Doctor_Treatment_1_Name",       "Qualification/Doctor_Diagnosis_2_Name",       "Qualification/Doctor_Treatment_2_Name"         } },
            { "Surgeon",            new List<string> { "Qualification/Doctor_Surgery_1_Name",         "Qualification/Doctor_Surgery_2_Name",         "Qualification/Doctor_Surgery_3_Name",         "Qualification/Doctor_Surgery_4_Name",         "Qualification/Doctor_Surgery_5_Name"           } },
            { "Researcher",         new List<string> { "Qualification/Doctor_Research_1_Name",        "Qualification/Doctor_Research_2_Name",        "Qualification/Doctor_Research_3_Name",        "Qualification/Doctor_Research_4_Name",        "Qualification/Doctor_Research_5_Name"          } },
            { "Psychiatrist",       new List<string> { "Qualification/Doctor_Psychiatry_1_Name",      "Qualification/Doctor_Psychiatry_2_Name",      "Qualification/Doctor_Psychiatry_3_Name",      "Qualification/Doctor_Psychiatry_4_Name",      "Qualification/Doctor_Psychiatry_5_Name"        } },

            // Nurses
            { "Diagnostics",        new List<string> { "Qualification/Doctor_Diagnosis_1_Name",       "Qualification/Doctor_Diagnosis_2_Name",       "Qualification/Doctor_Diagnosis_3_Name",       "Qualification/Doctor_Diagnosis_4_Name",       "Qualification/Doctor_Diagnosis_5_Name"         } },
            { "Ward Nurse",         new List<string> { "Qualification/Nurse_WardManagement_1_Name",   "Qualification/Nurse_WardManagement_2_Name",   "Qualification/Nurse_WardManagement_3_Name",   "Qualification/Nurse_WardManagement_4_Name",   "Qualification/Nurse_WardManagement_4_Name"     } },
            { "Pharmacist",         new List<string> { "Qualification/Nurse_Pharmacy_1_Name",         "Qualification/Doctor_Treatment_1_Name",       "Qualification/Doctor_Treatment_2_Name",       "Qualification/Doctor_Treatment_3_Name",       "Qualification/Doctor_Treatment_4_Name"         } },
            { "Injection",          new List<string> { "Qualification/Nurse_Injections_1_Name",       "Qualification/Doctor_Treatment_1_Name",       "Qualification/Doctor_Treatment_2_Name",       "Qualification/Doctor_Treatment_3_Name",       "Qualification/Doctor_Treatment_4_Name"         } },

            // Assistants
            { "Customer Service",   new List<string> { "Qualification/Assistant_Service_1_Name",      "Qualification/Assistant_Service_2_Name",      "Qualification/Assistant_Service_3_Name",      "Qualification/Assistant_Service_4_Name",      "Qualification/Assistant_Service_5_Name"        } },
            { "Marketer",           new List<string> { "Qualification/Assistant_Marketing_1_Name",    "Qualification/Assistant_Marketing_2_Name",    "Qualification/Assistant_Marketing_3_Name",    "Qualification/Assistant_Marketing_4_Name",    "Qualification/Assistant_Marketing_5_Name"      } },

            // Janitors
            { "Ghost Mechanic",     new List<string> { "Qualification/Janitor_GhostCapture_1_Name",   "Qualification/General_Speed_1_Name",          "Qualification/Janitor_Mechanics_1_Name",      "Qualification/Janitor_Mechanics_2_Name",      "Qualification/Janitor_Mechanics_3_Name"        } },
            { "Ghost Repair",       new List<string> { "Qualification/Janitor_GhostCapture_1_Name",   "Qualification/General_Speed_1_Name",          "Qualification/Janitor_Maintenance_1_Name",    "Qualification/Janitor_Maintenance_2_Name",    "Qualification/Janitor_Maintenance_3_Name"      } },
            { "Mechanic",           new List<string> { "Qualification/General_Speed_1_Name",          "Qualification/Janitor_Mechanics_1_Name",      "Qualification/Janitor_Mechanics_2_Name",      "Qualification/Janitor_Mechanics_3_Name",      "Qualification/Janitor_Mechanics_4_Name"        } },
            { "Repair",             new List<string> { "Qualification/General_Speed_1_Name",          "Qualification/Janitor_Maintenance_1_Name",    "Qualification/Janitor_Maintenance_2_Name",    "Qualification/Janitor_Maintenance_3_Name",    "Qualification/Janitor_Maintenance_4_Name"      } },

            // Terminator
            { "", new List<string> { } }
        };

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            HarmonyInstance harmony = HarmonyInstance.Create(modEntry.Info.Id);
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

        private static bool CheckMinAction(int i)
        {
            // 0 = off, 1 = students, 2 = random, 3 = max
            // checks to see if we should perform an action at a given level for the current state
            if (InInitialisePools  && i <= settings.RespecInitialPool) return true;
            if (!InInitialisePools && i <= settings.RespecFutureApplicants) return true;
            return false;
        }

        private static bool CheckExactAction(int i)
        {
            // 0 = off, 1 = students, 2 = random, 3 = max
            // checks to see if we should perform an action at a given level for the current state
            if (InInitialisePools && i == settings.RespecInitialPool) return true;
            if (!InInitialisePools && i == settings.RespecFutureApplicants) return true;
            return false;
        }

        public static bool ProcessJobApplicant(ref JobApplicant jobApplicant, string build)
        {
            if (!Builds.ContainsKey(build))
                return false;

            Traverse q = Traverse.Create(jobApplicant).Property("Qualifications");
            List<QualificationSlot> target = q.GetValue<List<QualificationSlot>>();
            target.Clear();

            if (CheckExactAction(1)) // we're speccing trainees
            {
                Traverse r = Traverse.Create(jobApplicant).Property("Rank");
                int rank = r.GetValue<int>();
                rank = 0;
                r.SetValue(rank);
            }
            else if (CheckMinAction(2)) // if we're set to random or max
            {
                int num = jobApplicant.MaxQualifications - 1;
                if (CheckExactAction(3))
                {
                    Traverse r = Traverse.Create(jobApplicant).Property("Rank");
                    int rank = r.GetValue<int>();
                    rank = 4;
                    r.SetValue(rank);

                    Traverse x = Traverse.Create(jobApplicant).Property("Experience");
                    float xp = x.GetValue<float>();
                    xp = 0.0f;
                    x.SetValue(xp);

                    num = jobApplicant.MaxQualifications;
                }
                else if (RandomUtils.GlobalRandomInstance.Next(0, 100) > 50)
                {
                    num++;
                }
                int i = 0;
                foreach (string job in Builds[build])
                {
                    if (++i > num) break;
                    if (!qualifications.Definitions.ContainsKey(job))
                    {
                        Logger.Error($"Unable to locate qualification {job} in current level.");
                        return false;
                    }
                    target.Add(new QualificationSlot(qualifications.Definitions[job], true));
                }
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
            Main.InInitialisePools = true;
            if (!Main.enabled || Main.settings.RespecInitialPool == 0)
                return;
            Main.InInitialisePools = true;
            Main.qualifications.Process(__instance.Qualifications);
        }

        static void Postfix(JobApplicantManager __instance)
        {
            if (!Main.enabled || Main.settings.RespecInitialPool == 0)
            {
                Main.InInitialisePools = false;
                return;
            }
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

        static void Postfix(JobApplicant __instance, WeightedList<QualificationDefinition> qualifications)
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
