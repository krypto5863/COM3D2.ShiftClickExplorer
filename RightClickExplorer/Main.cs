using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ShiftClickExplorer
{
	[BepInPlugin("ShiftClickExplorer", "ShiftClickExplorer", "1.3.1")]
	public class Main : BaseUnityPlugin
	{
		internal static ManualLogSource logger;

		public static ConfigEntry<KeyboardShortcut> ModifierKey;
		public static ConfigEntry<KeyboardShortcut> ModifierKey2;
		internal static readonly TextEditor editor = new TextEditor();

		private static Harmony harmony;
		private static Lookup<string, string> filesLookup;
		private static Lookup<string, string> presetsLookup;

		private static Lookup<string, string> FilesLookup =>
			filesLookup ?? (filesLookup =
				(Lookup<string, string>)Directory.GetFiles(Paths.GameRootPath + "\\Mod", "*.*", SearchOption.AllDirectories)
					.Where(t => t.ToLower().EndsWith(".menu") || t.ToLower().EndsWith(".mod"))
					.ToLookup(path => Path.GetFileName(path), path => path, StringComparer.OrdinalIgnoreCase));

		private static Lookup<string, string> PresetsLookup =>
			presetsLookup ?? (presetsLookup =
				(Lookup<string, string>)Directory.GetFiles(Paths.GameRootPath + "\\Preset", "*.preset", SearchOption.AllDirectories)
					.ToLookup(presetPath => Path.GetFileName(presetPath), presetPath => presetPath, StringComparer.OrdinalIgnoreCase));

		public void Awake()
		{
			harmony = Harmony.CreateAndPatchAll(typeof(Main));

			logger = this.Logger;

			ModifierKey = Config.Bind("General", "Open Containing Folder", new KeyboardShortcut(KeyCode.LeftShift), "The key to hold while clicking a menu item for shift-click explorer. This one only opens the folder in which your menu file is found if it's a mod file.");

			ModifierKey2 = Config.Bind("General", "Open File", new KeyboardShortcut(KeyCode.LeftShift, KeyCode.LeftControl), "The key to hold while clicking a menu item for shift-click explorer. This one opens the file itself if available.");

			UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode e)
		{
			if (s.name.Equals("SceneEdit"))
			{
				filesLookup = null;
				presetsLookup = null;
			}
		}

		private void OnDestroy()
		{
			filesLookup = null;
			presetsLookup = null;
			harmony?.UnpatchSelf();
			UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		[HarmonyPatch(typeof(SceneEdit), "ClickCallback")]
		[HarmonyPrefix]
		private static bool HandleShiftClick()
		{
			bool Modifier1 = ModifierKey.Value.IsDown() || ModifierKey.Value.IsPressed();
			bool Modifier2 = ModifierKey2.Value.IsDown() || ModifierKey2.Value.IsPressed();

#if DEBUG

			logger.LogDebug($"Was called on a menu! {Modifier1} || {Modifier2}");
#endif

			if (Modifier1 || Modifier2)
			{
				ButtonEdit componentInChildren = UIButton.current.GetComponentInChildren<ButtonEdit>();

				if (componentInChildren.m_MenuItem != null)
				{
					var menu = componentInChildren.m_MenuItem.m_strMenuFileName;

					if (!menu.IsNullOrWhiteSpace() && !Path.GetFileNameWithoutExtension(menu).Equals(menu))
					{
#if DEBUG
						logger.LogDebug($"Key was pressed, checking {menu}");
#endif
						var files = FilesLookup[menu];
#if DEBUG
						logger.LogDebug($"Checking file count of: {menu}");
#endif

						if (files.Count() > 0)
						{
#if DEBUG
							logger.LogDebug($"{menu} does exist in mod directory....");
#endif

							if (files.Count() > 1)
							{
								logger.LogWarning($"{menu} has duplicates in your mod directory! Multiple windows will open as a result.");
							}

							foreach (string s in files)
							{
								if (File.Exists(s))
								{
#if DEBUG
									logger.LogDebug($"Opening window at {s}");
#endif
									if (Modifier2)
									{
										Process.Start(s);
									}
									else
									{
										Process.Start("explorer.exe", "/select, " + s);
									}
								}
							}
						}
						else
						{
							logger.LogInfo($"{menu} may not be a mod file or it isn't found in the Mod directory. The file name will be copied to your clipboard instead!");
						}
#if DEBUG
						logger.LogDebug($"Done, copying {menu} to clipboard...");
#endif

						CopyToClipboard(menu);
					}
					return false;
				}
			}
			return true;
		}
		
		[HarmonyPatch(typeof(PresetMgr), "ClickPreset")]
		[HarmonyPrefix]
		private static bool HandleShiftClickPreset()
		{
			bool Modifier1 = ModifierKey.Value.IsDown() || ModifierKey.Value.IsPressed();

			if (Modifier1)
			{
				string presetName = UIButton.current.name;

				if (presetName.EndsWith(".preset"))
				{
#if DEBUG
					logger.LogDebug($"Current selected preset {presetName}");
#endif
					var presets = PresetsLookup[presetName];

#if DEBUG
					logger.LogDebug($"Checking file count of: {presetName}");
#endif

					if (presets.Count() > 0)
					{
#if DEBUG
						logger.LogDebug($"{presetName} does exist in preset directory....");
#endif

						if (presets.Count() > 1)
						{
							logger.LogWarning(
								$"{presetName} has duplicates in your preset directory! Multiple windows will open as a result.");
						}

						foreach (string s in presets)
						{
							if (File.Exists(s))
							{
#if DEBUG
								logger.LogDebug($"Opening window at {s}");
#endif
								Process.Start("explorer.exe", "/select, " + s);
							}
						}
					}
					else
					{
						logger.LogInfo(
							$"{presetName} may not be a mod file or it isn't found in the Mod directory. The file name will be copied to your clipboard instead!");
					}
#if DEBUG
					logger.LogDebug($"Done, copying {presetName} to clipboard...");
#endif

					CopyToClipboard(presetName);
				}
				return false;
			}
			return true;
		}
		
		
		public static void CopyToClipboard(string s)
		{
			editor.text = s;
			editor.SelectAll();
			editor.Copy();
		}
	}
}
