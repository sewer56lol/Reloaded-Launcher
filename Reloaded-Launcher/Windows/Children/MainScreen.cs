﻿/*
    [Reloaded] Mod Loader Launcher
    The launcher for a universal, powerful, multi-game and multi-process mod loader
    based off of the concept of DLL Injection to execute arbitrary program code.
    Copyright (C) 2018  Sewer. Sz (Sewer56)

    [Reloaded] is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    [Reloaded] is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>
*/

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Reloaded.IO.Config;
using Reloaded.Paths;
using ReloadedLauncher.Windows.Children.Dialogs;
using Reloaded_GUI.Utilities.Controls;
using Bindings = Reloaded_GUI.Bindings;

namespace ReloadedLauncher.Windows.Children
{
    public partial class MainScreen : Form
    {
        /// <summary>
        /// Constructor for this class.
        /// Requires the specification of the MDI Parent
        /// form that hosts this window in question.
        /// </summary>
        /// <param name="mdiParent">The MDI Parent form, an instance of Base.cs</param>
        public MainScreen(Form mdiParent)
        {
            // Standard Winforms Initialization
            InitializeComponent();

            // Set the MDI parent
            MdiParent = mdiParent;

            // Add to the window list.
            Bindings.WindowsForms.Add(this);

            // Add Box Controls
            SetupDecorationBoxes.FindDecorationControls(this);
        }

        /// <summary>
        /// Changes the titlebar & other properties when the form visibility of the form changes.
        /// </summary>
        private void Main_Screen_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                // Update title
                Global.CurrentMenuName = Strings.Launcher.Menus.MainMenuName;
                Global.BaseForm.UpdateTitle("");

                // Set version
                item_VersionBoxVersion.Text = Application.ProductVersion;
                item_InjectionBoxInjection.Text = Convert.ToString(File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location), CultureInfo.InvariantCulture);

                // Load games.
                LoadGames();

                // Re-select the last selected game.
                ReselectCurrentGame();
            }
            else
            {
                SaveLastGame();
            }
        }

        /// <summary>
        /// Saves the last selected game such that it may be re-selected on launch.
        /// </summary>
        public void SaveLastGame()
        {
            // No game config selected (yet) at initialization.
            if (Global.CurrentGameConfig == null) return;

            // Write the loader config with the last saved game.
            Global.LoaderConfiguration.LastGameFolder = Global.CurrentGameConfig.ConfigLocation;
            LoaderConfig.WriteConfig(Global.LoaderConfiguration);
        }

        /// <summary>
        /// Retrieves the list of games from their configurations.
        /// </summary>
        private void LoadGames()
        {
            // Clear the current listview.
            box_GameList.Rows.Clear();

            // Retrieve current game list the into Global.
            Global.GameConfigurations = ConfigManager.GetAllGameConfigs();

            // For each config, append it.
            foreach (GameConfig gameConfig in Global.GameConfigurations)
            {
                // Stores the path of the mod for display.
                string modPath = LoaderPaths.GetModLoaderModDirectory() + "\\" + gameConfig.ModDirectory;

                // Retrieves the relative path for presentation.
                string relativeModPath = LoaderPaths.GetModLoaderRelativePath(modPath);

                // Add the relative path.
                box_GameList.Rows.Add(gameConfig.GameName, relativeModPath);
            }
        }

        /// <summary>
        /// Re-selects the last selected game upon entering the Main Menu.
        /// </summary>
        private void ReselectCurrentGame()
        {
            int currentRowIndex = 0;

            // Find row of last selected game.
            for (; currentRowIndex < Global.GameConfigurations.Count; currentRowIndex++)
            {
                // Select the row if there is a directory match.
                if (Global.GameConfigurations[currentRowIndex].ConfigLocation == Global.LoaderConfiguration.LastGameFolder)
                {
                    box_GameList.Rows[currentRowIndex].Selected = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Load the relevant game details when the selection is changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GameList_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                // Retrieve the current game details.
                Global.CurrentGameConfig = Global.GameConfigurations[box_GameList.SelectedCells[0].RowIndex];

                // Update note box.
                item_NoteBoxEXEPath.Text = Global.CurrentGameConfig.ExecutableLocation.Substring(Global.CurrentGameConfig.ExecutableLocation.IndexOf("/", StringComparison.Ordinal) + 1);
                item_NoteBoxVerPath.Text = Global.CurrentGameConfig.GameVersion;
                item_NoteBoxGameName.Text = Global.CurrentGameConfig.GameName;

                // Update location box.
                item_LocationBoxDirectoryPath.Text = Global.CurrentGameConfig.GameDirectory;
                item_LocationBoxEXEPath.Text = "$DIRECTORY + " + Global.CurrentGameConfig.ExecutableLocation;

                // Load the game image.
                try
                {
                    // Doing this the roundabout way to not leave file handles open.
                    Image image;
                    using (var bmpTemp = new Bitmap(GameConfig.GetBannerPath(Global.CurrentGameConfig)))
                    { image = new Bitmap(bmpTemp); }

                    item_GameBanner.BackgroundImage = image;
                }
                catch { item_GameBanner.BackgroundImage = null; }
            }
            catch { }
        }

        /// <summary>
        /// Quits the mod loader.
        /// </summary>
        private void QuitBox_Click(object sender, EventArgs e)
        {
            // Remember last selected game.
            SaveLastGame();

            // Shutdown program.
            Functions.Shutdown();
        }

        /// <summary>
        /// Launches Reloaded-Loader.exe in the same directory as the launcher.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void item_LaunchBox_MouseClick(object sender, MouseEventArgs e)
        {
            // Remember last selected game.
            SaveLastGame();

            // Check if global config
            if (Global.CurrentGameConfig.ConfigLocation == GameConfig.GetGlobalConfig().ConfigLocation)
            {
                MessageBox.Show("One does not launch the Global Mod Configuration, you can try though...");
                return;
            }

            if ((ModifierKeys & Keys.Control) != 0)
            {
                LaunchGameDialog launchGameGenericDialog = new LaunchGameDialog();
                launchGameGenericDialog.ShowDialog();
            }
            else { LaunchGame(); }
        }

        /// <summary>
        /// Launches the currently selected game configuration with predefined default settings.
        /// </summary>
        private void LaunchGame()
        {
            Functions.LaunchLoader(new string[0]);
        }
    }
}
