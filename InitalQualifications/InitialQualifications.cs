using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using UnityModManagerNet;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TH20;
using FullInspector;
using TMPro;

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
        static Dictionary<StaffDefinition.Type, List<List<string>>> pools = new Dictionary<StaffDefinition.Type, List<List<string>>>() {
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

}
