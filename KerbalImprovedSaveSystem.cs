﻿using KerbalImprovedSaveSystem.Extensions;
using KSP.IO;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace KerbalImprovedSaveSystem
{
	/// <summary>
	/// Start Kerbal Improved Save System when in a flight scene.
	/// </summary>
	[KSPAddon(KSPAddon.Startup.Flight, false)] 
	public class KerbalImprovedSaveSystem : MonoBehaviour
	{
		// used to identify log entries of this plugin
		public static string modLogTag = "[KISS]: ";

		// stuff to configure the GUI
		private static Rect _windowPosition = new Rect();
		private static bool _isVisible = false;
		private GUIStyle _windowStyle, _labelStyle, _buttonStyle, _altBtnStyle, _listBtnStyle, _listSelectionStyle, _txtFieldStyle, _listStyle;
		private bool _hasInitStyles = false;

		// stuff to detect double clicks
		private bool dblClicked = false;
		private DateTime lastClickTime;
		public TimeSpan catchTime = TimeSpan.FromMilliseconds(250);

		// savegame directory of the current came
		private string saveGameDir;
		// currently selected filename for savegame
		private string selectedFileName = "";
		// list of existing savegames
		private List<string> existingSaveGames;
		// flag to enforce/disable overwrite confirmations
		private bool confirmOverwrite = false;
		// scroll position in the list of existing savegames
		private Vector2 _scrollPos;


		/// <summary>
		/// Main part of this Add-on; used to react to keyboard input and do all the magic stuff :)
		/// </summary>
		void Update()
		{
			if (!_isVisible)
			{
				// show window on F8
				if (Input.GetKey(KeyCode.F8)) //GameSettings.QUICKSAVE.GetKey() && GameSettings.MODIFIER_KEY.GetKey())
				{
					if (!_hasInitStyles)
					{
						Debug.Log(modLogTag + "Init GUI.");
						InitStyles();
					}
						
					FlightDriver.SetPause(true);
					_isVisible = true;
					saveGameDir = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/";
					existingSaveGames = getExistingSaves(saveGameDir);
					selectedFileName = DateTime.Now.ToString("yyyyMMdd_HHmmss_") + FlightGlobals.ActiveVessel.vesselName;
					RenderingManager.AddToPostDrawQueue(0, OnDraw);
				}

				// detect double clicks //not working when paused... (no events?)
//				if (Input.GetMouseButtonDown(0))
//				{
//					if (Time.time - lastClickTime < catchTime)
//					{
//						//double click
//						Debug.Log(modLogTag + "Double click");
//						dblClicked = true;
//					} else
//					{
//						//normal click
//					}
//					lastClickTime = Time.time;
//				}

			} else // if visible...
			{
				// detect double clicks
				if (Input.GetMouseButtonDown(0))
				{
					if (DateTime.Now - lastClickTime < catchTime)
					{
						dblClicked = true;
					} else
					{
						//normal click
						dblClicked = false;
					}
					lastClickTime = DateTime.Now;
				}

				// allow aborting window by pressing ESC
				if (Input.GetKey(KeyCode.Escape))
				{
					Close("SaveDialog aborted by user.");
				}
			}
		}


		/// <summary>
		/// Raises the draw event and handles window positioning.
		/// </summary>
		private void OnDraw()
		{
			if (_windowPosition.x == 0f && _windowPosition.y == 0f)
			{
				PluginConfiguration config = PluginConfiguration.CreateForType<KerbalImprovedSaveSystem>();
				config.load();
				_windowPosition = config.GetValue<Rect>("Window Position", _windowPosition.CenterScreen());
			}

			_windowPosition = GUILayout.Window(1337, _windowPosition, OnWindow, "Kerbal Improved Save System", _windowStyle);
		}


		/// <summary>
		/// Handles all the GUI drawing/layout.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		private void OnWindow(int windowId)
		{

			GUILayout.BeginVertical();
			GUILayout.Label("Existing savegames:", _labelStyle);

			_scrollPos = GUILayout.BeginScrollView(_scrollPos, _listStyle);
			int i = 0;
			if (existingSaveGames == null)
				Debug.Log(modLogTag + "No existing savegames found.");
			else
			{
				for (; i < existingSaveGames.Count; i++)
				{
					var rect = new Rect(5, 20 * i, 285, 20);
					if (rect.yMax < _scrollPos.y || rect.yMin > _scrollPos.y + 500)
					{
						//do not draw items outside the current ScrollView
						continue;
					}

					string saveGameName = existingSaveGames[i];
					GUIStyle _renderStyle = _listBtnStyle;
					if (saveGameName == selectedFileName)
					{
						// highlight the list item that is currently selected
						_renderStyle = _listSelectionStyle;
					}
					if (GUILayout.Button(saveGameName, _renderStyle))
					{
						selectedFileName = saveGameName;
						if (dblClicked)
						{
							dblClicked = false;
							Save(selectedFileName);
							Close("SaveDialog completed.");
						}
					}
				}
			}
			GUILayout.Space(20 * i);
			GUILayout.EndScrollView();

			GUILayout.Label("Save game as:", _labelStyle);
			selectedFileName = GUILayout.TextField(selectedFileName, _txtFieldStyle);

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Now + shipname", _altBtnStyle))
			{
				selectedFileName = DateTime.Now.ToString("yyyyMMdd_HHmmss_") + FlightGlobals.ActiveVessel.vesselName;
			}
			GUILayout.FlexibleSpace(); // moves the following buttons to the right
			if (GUILayout.Button("Cancel", _buttonStyle))
			{
				Close("SaveDialog aborted by user.");
			}
			if (GUILayout.Button("Save", _buttonStyle))
			{
				Save(selectedFileName);
				Close("SaveDialog completed.");
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();

			GUI.DragWindow();
		}


		/// <summary>
		/// Initialises all required GUIStyles for the plugin.
		/// </summary>
		private void InitStyles()
		{
			_windowStyle = new GUIStyle(HighLogic.Skin.window);
			_windowStyle.fixedWidth = 400f;
			_windowStyle.fixedHeight = 500f;
			_windowStyle.normal.textColor = Color.yellow;
			_windowStyle.onNormal.textColor = Color.yellow;
			_windowStyle.hover.textColor = Color.yellow;
			_windowStyle.onHover.textColor = Color.yellow;
			_windowStyle.active.textColor = Color.yellow;
			_windowStyle.onActive.textColor = Color.yellow;
			_windowStyle.padding.left = 6;
			_windowStyle.padding.right = 6;

			_labelStyle = new GUIStyle(HighLogic.Skin.label);
			_labelStyle.stretchWidth = true;

			_buttonStyle = new GUIStyle(HighLogic.Skin.button);

			_altBtnStyle = new GUIStyle(HighLogic.Skin.button);
			_altBtnStyle.normal.textColor = Color.yellow;
			_altBtnStyle.hover.textColor = Color.yellow;
			_altBtnStyle.active.textColor = Color.yellow;

			_listBtnStyle = new GUIStyle(HighLogic.Skin.button);
			_listBtnStyle.hover.background = _listBtnStyle.normal.background;
			_listBtnStyle.normal.background = null;

			_listSelectionStyle = new GUIStyle(HighLogic.Skin.button);
			_listSelectionStyle.normal.background = _listSelectionStyle.active.background;
			_listSelectionStyle.hover.background = _listSelectionStyle.active.background;
			_listSelectionStyle.normal.textColor = Color.yellow;
			_listSelectionStyle.hover.textColor = Color.yellow;

			_txtFieldStyle = new GUIStyle(HighLogic.Skin.textField);
			_txtFieldStyle.stretchWidth = true;

			_listStyle = new GUIStyle(HighLogic.Skin.scrollView);
			_listStyle.padding.left = 6;
			_listStyle.padding.right = 6;
			_listStyle.margin.left = 4;
			_listStyle.margin.right = 4;

			_hasInitStyles = true;
		}


		/// <summary>
		/// Gets the existing savegame filenames (*.sfs) in the specified directory WITHOUT the 
		/// special "persistent.sfs" file (that is special in KSP and will be overwritten
		/// every time a savegame is loaded anyway).
		/// </summary>
		/// <returns>List of existing savegames without their paths and extensions.</returns>
		/// <param name="saveFolder">Directory to search.</param>
		private List<string> getExistingSaves(string saveDir)
		{
			List<string> saveGames = null;

			// retrieve all savegames and put them into a list
			if (Directory.Exists(saveDir))
			{
				Debug.Log(modLogTag + "saveDir found!");
				string[] saves = Directory.GetFiles(saveDir, "*.sfs");
				saveGames = new List<string>(saves.Length);
				foreach (string sav in saves)
				{
					// ignore "persistent.sfs" because that is special in KSP
					// (and will be overwritten every time a savegame is loaded anyway)
					if (!sav.Equals(saveDir + "persistent.sfs"))
					{
						// remove path and ".sfs" from filenames and add them to list
						saveGames.Add(sav.Substring(sav.LastIndexOf("/") + 1).Replace(".sfs", ""));
					}
				}
				saveGames.Sort();
			}

			return saveGames;
		}


		/// <summary>
		/// Save the current game progress/status into the specified filename.
		/// </summary>
		/// <param name="selectedSaveFileName">File name to save the game into.</param>
		private void Save(string selectedSaveFileName)
		{
			if (confirmOverwrite && existingSaveGames.Contains(selectedFileName))
			{
				// TODO dialog asking for confirmation before overwriting file
//				KISSDialog confirm = new KISSDialog();
//				confirm.Show(selectedFileName);
			} else
			{
				SaveMode s = SaveMode.OVERWRITE; // available SaveModes are: OVERWRITE, APPEND, ABORT
				string filename = GamePersistence.SaveGame(selectedSaveFileName, HighLogic.SaveFolder, s);
				Debug.Log(modLogTag + "Game saved in '" + filename + "'");
			}
		}


		/// <summary>
		/// Closes the KISS window and writes the specified reason into the Debug.Log.
		/// </summary>
		/// <param name="reason">Why/How was the window closed?</param>
		private void Close(string reason)
		{
			// save window position into config file
			PluginConfiguration config = PluginConfiguration.CreateForType<KerbalImprovedSaveSystem>();
			config.SetValue("Window Position", _windowPosition);
			config.save();

			// code to remove window from UI
			_isVisible = false;
			RenderingManager.RemoveFromPostDrawQueue(0, OnDraw);
			Debug.Log(modLogTag + reason);	
			FlightDriver.SetPause(false);
		}
	}
}

