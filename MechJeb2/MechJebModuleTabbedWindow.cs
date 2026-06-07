extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleTabbedWindow : DisplayModule
    {
        [UsedImplicitly]
        public MechJebModuleTabbedWindow(MechJebCore core) : base(core) { }

        public override string GetName() => "Tabbed Windows";

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ShowTabBar = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public string SelectedTabName = "";

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool RememberTabs = true;

        public List<string> TabModuleNames { get; private set; } = new List<string>();
        public string[] AvailableModules { get; private set; } = Array.Empty<string>();

        private string _newTabToAdd = "";
        private Vector2 _scrollPos;
        private int _selectedTabIndex = -1;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            RefreshAvailableModules();
        }

        public void RefreshAvailableModules()
        {
            // Get all available DisplayModule types
            var displayTypes = AssemblyLoader.loadedAssemblies
                .SelectMany(a => a.assembly.GetExportedTypes())
                .Where(t => t.IsSubclassOf(typeof(DisplayModule)) && t != typeof(MechJebModuleTabbedWindow))
                .Select(t => t.Name)
                .OrderBy(n => n)
                .ToArray();
            AvailableModules = displayTypes;
        }

        public void AddTab(string moduleName)
        {
            if (!string.IsNullOrEmpty(moduleName) && !TabModuleNames.Contains(moduleName))
            {
                TabModuleNames.Add(moduleName);
                if (TabModuleNames.Count == 1)
                    SelectedTabName = moduleName;
                _selectedTabIndex = TabModuleNames.IndexOf(moduleName);
            }
        }

        public void RemoveTab(string moduleName)
        {
            TabModuleNames.Remove(moduleName);
            if (TabModuleNames.Count == 0)
            {
                SelectedTabName = "";
                _selectedTabIndex = -1;
            }
            else if (SelectedTabName == moduleName || !TabModuleNames.Contains(SelectedTabName))
            {
                SelectedTabName = TabModuleNames[0];
                _selectedTabIndex = 0;
            }
        }

        public void SelectTab(string moduleName)
        {
            if (TabModuleNames.Contains(moduleName))
            {
                SelectedTabName = moduleName;
                _selectedTabIndex = TabModuleNames.IndexOf(moduleName);
            }
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            if (ShowTabBar && TabModuleNames.Count > 0)
            {
                GUILayout.BeginHorizontal();

                for (int i = 0; i < TabModuleNames.Count; i++)
                {
                    string tabName = TabModuleNames[i];
                    bool isSelected = tabName == SelectedTabName;

                    GUI.color = isSelected ? Color.green : Color.grey;

                    if (GUILayout.Button(tabName.Replace("MechJebModule", "").Replace("Window", "")))
                    {
                        SelectTab(tabName);
                    }

                    GUI.color = Color.white;
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }

            // Show info about the currently selected module
            if (!string.IsNullOrEmpty(SelectedTabName) && TabModuleNames.Contains(SelectedTabName))
            {
                var module = Core.GetComputerModule(SelectedTabName) as DisplayModule;
                if (module != null)
                {
                    GUILayout.Label($"Active: {module.GetName()}");

                    if (!module.Enabled)
                    {
                        if (GUILayout.Button("Enable Module Window"))
                        {
                            module.Enabled = true;
                            module.Users.Add(this);
                        }
                    }
                    else
                    {
                        GUILayout.Label("(window is visible)");
                    }
                }
                else
                {
                    GUILayout.Label($"Module '{SelectedTabName}' not found.");
                }
            }
            else
            {
                GUILayout.Label("No tab selected. Add tabs below.");
            }

            // Tab management section
            GUILayout.Space(10);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            GUILayout.Label("Tab Management:", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(100));

            GUILayout.BeginHorizontal();
            _newTabToAdd = GUILayout.TextField(_newTabToAdd, GUILayout.Width(150));

            if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
            {
                if (!string.IsNullOrEmpty(_newTabToAdd))
                {
                    AddTab(_newTabToAdd);
                    _newTabToAdd = "";
                }
            }
            GUILayout.EndHorizontal();

            // Show buttons for available modules
            if (AvailableModules.Length > 0)
            {
                GUILayout.Label("Quick-add modules:", GUILayout.ExpandWidth(true));
                GUILayout.BeginHorizontal();
                foreach (string modName in AvailableModules.Take(5))
                {
                    if (!TabModuleNames.Contains(modName) && GUILayout.Button(modName.Replace("MechJebModule", "").Replace("Window", "")))
                    {
                        AddTab(modName);
                    }
                }
                GUILayout.EndHorizontal();
            }

            // List current tabs
            for (int i = TabModuleNames.Count - 1; i >= 0; i--)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(TabModuleNames[i], GUILayout.ExpandWidth(true));
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    RemoveTab(TabModuleNames[i]);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            ShowTabBar = GUILayout.Toggle(ShowTabBar, "Show tab bar");
            RememberTabs = GUILayout.Toggle(RememberTabs, "Remember tabs");

            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }
    }
}
