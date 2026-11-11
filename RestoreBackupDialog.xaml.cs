using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MEABasicSaveEditor
{
    public partial class RestoreBackupDialog : Window
    {
        private readonly SaveManager _mgr;
        private readonly string _activeLivePath;

        public bool BackupLoaded { get; private set; } = false;
        public string SelectedBackupPath { get; private set; }
        public string SelectedTargetLivePath { get; private set; }

        public RestoreBackupDialog(SaveManager mgr, string activeLivePath)
        {
            InitializeComponent();
            _mgr = mgr;
            _activeLivePath = activeLivePath;

            LoadTargetSaves();
            LoadBackups();
        }

        private void LoadTargetSaves()
        {
            var liveSaves = _mgr.GetLiveSaves();
            CmbTargetLiveSave.ItemsSource = liveSaves;

            if (liveSaves.Count > 0)
            {
                // If there's an active live path matching one of the items, pre-select it
                if (!string.IsNullOrEmpty(_activeLivePath))
                {
                    var match = liveSaves.FirstOrDefault(s => string.Equals(s.FullPath, _activeLivePath, StringComparison.OrdinalIgnoreCase)
                                                           || string.Equals(s.FileName, Path.GetFileName(_activeLivePath), StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        CmbTargetLiveSave.SelectedItem = match;
                        return;
                    }
                }

                // Otherwise pre-select the most recently modified live save
                CmbTargetLiveSave.SelectedIndex = 0;
            }
        }

        private void LoadBackups()
        {
            var list = _mgr.GetBackups();
            DgBackups.ItemsSource = list;
            TxtBackupCount.Text = $"{list.Count} backup file(s) available in 'ME Andromeda Save Editor Backup'";
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadTargetSaves();
            LoadBackups();
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(SaveManager.BackupDirectory))
            {
                Process.Start("explorer.exe", SaveManager.BackupDirectory);
            }
            else
            {
                MessageBox.Show("Backup directory does not exist yet:\n" + SaveManager.BackupDirectory, "Backups", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnRestoreItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is BackupItemModel item)
            {
                var targetItem = CmbTargetLiveSave.SelectedItem as LiveSaveItemModel;
                if (targetItem == null)
                {
                    MessageBox.Show("Please select a target live game save to overwrite from the dropdown at the top.", "No Target Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show(
                    $"Load this backup into the editor?\n\n" +
                    $"• Backup File: {item.FileName}\n" +
                    $"• Backup Date: {item.FormattedDate}\n\n" +
                    $"• Target Live Game Save to Overwrite: {targetItem.FileName}\n\n" +
                    $"All values from this backup will populate the editor. You can review them, and clicking '💾 Save' on the main window will overwrite '{targetItem.FileName}' in your live game folder.",
                    "Load Backup into Editor",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    bool ok = _mgr.LoadBackupForRestore(item.FullPath, targetItem.FullPath);
                    if (ok)
                    {
                        BackupLoaded = true;
                        SelectedBackupPath = item.FullPath;
                        SelectedTargetLivePath = targetItem.FullPath;
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Failed to load backup file into editor.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
