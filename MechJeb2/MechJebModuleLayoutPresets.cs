extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

namespace MuMech
{
    /// <summary>
    /// Saves and restores window layout snapshots — which modules are open and their positions.
    /// </summary>
    public class MechJebModuleLayoutPresets : ComputerModule
    {
        [UsedImplicitly]
        public MechJebModuleLayoutPresets(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public string LastPresetName = "";

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int MaxPresets = 10;

        public class LayoutPreset
        {
            public string name;
            public List<string> enabledModules = new List<string>();
            public Dictionary<string, Vector4> windowPositions = new Dictionary<string, Vector4>();
            public string timestamp;
        }

        public List<LayoutPreset> Presets { get; private set; } = new List<LayoutPreset>();
        public string StatusText { get; private set; } = "";

        public string NewPresetName = "";

        private string PresetFilePath => Path.Combine(
            KSPUtil.ApplicationRootPath,
            "GameData",
            "MechJeb2",
            "Plugins",
            "LayoutPresets.cfg"
        );

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            LoadPresets();
        }

        public void SaveCurrentLayout(string presetName)
        {
            if (string.IsNullOrEmpty(presetName)) return;

            var preset = new LayoutPreset
            {
                name = presetName,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            // Capture enabled display modules and their positions
            var displayModules = Core.GetDisplayModules(MechJebModuleMenu.DisplayOrder.instance);
            foreach (var module in displayModules)
            {
                if (module.Enabled)
                {
                    preset.enabledModules.Add(module.GetType().Name);

                    // Save window position
                    preset.windowPositions[module.GetType().Name] = module.WindowVector;
                }
            }

            // Remove existing preset with same name
            Presets.RemoveAll(p => p.name == presetName);
            Presets.Add(preset);

            // Enforce max presets
            while (Presets.Count > MaxPresets)
                Presets.RemoveAt(0);

            SavePresets();
            LastPresetName = presetName;
            StatusText = $"Saved layout: {presetName} ({preset.enabledModules.Count} windows)";
        }

        public void LoadLayout(string presetName)
        {
            var preset = Presets.FirstOrDefault(p => p.name == presetName);
            if (preset == null)
            {
                StatusText = $"Preset '{presetName}' not found";
                return;
            }

            // First disable all display modules
            var allModules = Core.GetDisplayModules(MechJebModuleMenu.DisplayOrder.instance);
            foreach (var module in allModules)
            {
                module.Enabled = false;
            }

            // Then enable the ones in the preset
            foreach (string moduleName in preset.enabledModules)
            {
                var module = Core.GetComputerModule(moduleName) as DisplayModule;
                if (module != null)
                {
                    module.Enabled = true;
                    module.Users.Add(this);

                    // Restore window position
                    if (preset.windowPositions.TryGetValue(moduleName, out Vector4 pos))
                    {
                        module.WindowVector = pos;
                    }
                }
            }

            LastPresetName = presetName;
            StatusText = $"Loaded layout: {presetName} ({preset.enabledModules.Count} windows)";
        }

        public void DeletePreset(string presetName)
        {
            Presets.RemoveAll(p => p.name == presetName);
            SavePresets();
            if (LastPresetName == presetName)
                LastPresetName = Presets.Count > 0 ? Presets.Last().name : "";
            StatusText = $"Deleted preset: {presetName}";
        }

        public void SavePresets()
        {
            try
            {
                ConfigNode root = new ConfigNode("LAYOUT_PRESETS");
                foreach (var preset in Presets)
                {
                    ConfigNode node = new ConfigNode("PRESET");
                    node.AddValue("name", preset.name);
                    node.AddValue("timestamp", preset.timestamp);

                    ConfigNode modulesNode = node.AddNode("MODULES");
                    foreach (string moduleName in preset.enabledModules)
                    {
                        modulesNode.AddValue("module", moduleName);
                    }

                    ConfigNode positionsNode = node.AddNode("POSITIONS");
                    foreach (var kvp in preset.windowPositions)
                    {
                        ConfigNode posNode = positionsNode.AddNode("WINDOW");
                        posNode.AddValue("name", kvp.Key);
                        posNode.AddValue("x", kvp.Value.x);
                        posNode.AddValue("y", kvp.Value.y);
                        posNode.AddValue("z", kvp.Value.z);
                        posNode.AddValue("w", kvp.Value.w);
                    }

                    root.AddNode(node);
                }

                root.Save(PresetFilePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"MechJeb LayoutPresets save error: {ex.Message}");
            }
        }

        public void LoadPresets()
        {
            Presets.Clear();
            try
            {
                if (!File.Exists(PresetFilePath)) return;

                ConfigNode root = ConfigNode.Load(PresetFilePath);
                if (root == null) return;

                foreach (ConfigNode node in root.GetNodes("PRESET"))
                {
                    var preset = new LayoutPreset
                    {
                        name = node.GetValue("name"),
                        timestamp = node.GetValue("timestamp")
                    };

                    ConfigNode modulesNode = node.GetNode("MODULES");
                    if (modulesNode != null)
                    {
                        foreach (string moduleName in modulesNode.GetValues("module"))
                        {
                            preset.enabledModules.Add(moduleName);
                        }
                    }

                    ConfigNode positionsNode = node.GetNode("POSITIONS");
                    if (positionsNode != null)
                    {
                        foreach (ConfigNode posNode in positionsNode.GetNodes("WINDOW"))
                        {
                            string name = posNode.GetValue("name");
                            Vector4 pos = new Vector4(
                                float.Parse(posNode.GetValue("x")),
                                float.Parse(posNode.GetValue("y")),
                                float.Parse(posNode.GetValue("z")),
                                float.Parse(posNode.GetValue("w"))
                            );
                            preset.windowPositions[name] = pos;
                        }
                    }

                    Presets.Add(preset);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"MechJeb LayoutPresets load error: {ex.Message}");
            }
        }
    }
}
