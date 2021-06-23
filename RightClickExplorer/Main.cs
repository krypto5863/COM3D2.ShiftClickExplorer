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
	[BepInPlugin("ShiftClickExplorer", "ShiftClickExplorer", "1.0")]
	public class Main : BaseUnityPlugin
	{
		internal static ManualLogSource logger;

		public static ConfigEntry<KeyboardShortcut> ModifierKey;

		public void Awake()
		{
			Harmony.CreateAndPatchAll(typeof(Main));

			logger = this.Logger;

			ModifierKey = Config.Bind("General", "Key To Activate", new KeyboardShortcut(KeyCode.LeftShift), "The key to hold while clicking a menu item for shift-click explorer.");
		}

		[HarmonyPatch(typeof(SceneEdit), "ClickCallback")]
		[HarmonyPrefix]
		private static bool HandleShiftClick()
		{
			if (ModifierKey.Value.IsDown() || ModifierKey.Value.IsPressed())
			{
				ButtonEdit componentInChildren = UIButton.current.GetComponentInChildren<ButtonEdit>();

				if (componentInChildren.m_MenuItem != null)
				{
					var menu = componentInChildren.m_MenuItem.m_strMenuFileName;

					if (!menu.IsNullOrWhiteSpace() && !Path.GetFileNameWithoutExtension(menu).Equals(menu))
					{
						logger.LogDebug($"Key was pressed, checking {menu}");

						string[] files = Directory.GetFiles(BepInEx.Paths.GameRootPath + "\\Mod", menu, SearchOption.AllDirectories);

						logger.LogDebug($"Checking file count of: {menu}");

						if (files.Count() > 0)
						{
							logger.LogDebug($"{menu} does exist in mod directory....");

							if (files.Count() > 1)
							{
								logger.LogWarning($"{menu} has duplicates in your mod directory! Multiple windows will open as a result.");
							}

							foreach (string s in files)
							{
								if (File.Exists(s))
								{
									logger.LogDebug($"Opening window at {s}");

									Process.Start(Path.GetDirectoryName(s));
								}
							}
						}
						else
						{
							logger.LogInfo($"{menu} may not be a mod file or it isn't found in the Mod directory. The file name will be copied to your clipboard instead!");
						}

						logger.LogDebug($"Done, copying {menu} to clipboard...");

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
