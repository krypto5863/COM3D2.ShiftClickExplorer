using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
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
	[BepInPlugin("ShiftClickExplorer", "ShiftClickExplorer", "1.2")]
	public class Main : BaseUnityPlugin
	{
		internal static ManualLogSource logger;

		public static ConfigEntry<KeyboardShortcut> ModifierKey;
		public static ConfigEntry<KeyboardShortcut> ModifierKey2;

		static string[] Files;

		public void Awake()
		{
			Harmony.CreateAndPatchAll(typeof(Main));

			logger = this.Logger;

			ModifierKey = Config.Bind("General", "Key To Activate", new KeyboardShortcut(KeyCode.LeftShift), "The key to hold while clicking a menu item for shift-click explorer. This one only opens the folder in which your menu file is found.");

			ModifierKey2 = Config.Bind("General", "Key To Activate 2", new KeyboardShortcut(KeyCode.LeftShift, KeyCode.LeftControl), "The key to hold while clicking a menu item for shift-click explorer. This one opens the file itself if available.");

			UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s,e) => 
			{
				if (s.name.Equals("SceneEdit")) 
				{
					Files = null;
				}
			};
		}

		[HarmonyPatch(typeof(SceneEdit), "ClickCallback")]
		[HarmonyPrefix]
		private static bool HandleShiftClick()
		{
			bool Modifier1 = ModifierKey.Value.IsDown() || ModifierKey.Value.IsPressed();
			bool Modifier2 = ModifierKey2.Value.IsDown() || ModifierKey2.Value.IsPressed();

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
						if (Files == null) 
						{
							Files = Directory.GetFiles(BepInEx.Paths.GameRootPath + "\\Mod", "*.*", SearchOption.AllDirectories).Where(t => t.ToLower().EndsWith(".menu") || t.ToLower().EndsWith(".mod")).ToArray();
						}

						var files = Files.Where(file => Path.GetFileName(file).ToLower().Equals(menu.ToLower()));
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
		public static void CopyToClipboard(string s)
		{
			TextEditor te = new TextEditor();
			te.text = s;
			te.SelectAll();
			te.Copy();
		}
	}
}
