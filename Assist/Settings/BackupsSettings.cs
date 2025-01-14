﻿using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Assist.Objects.RiotClient;
using Assist.Views.Authentication.Sections.ViewModels;
using Serilog;
using YamlDotNet.Serialization.NamingConventions;

namespace Assist.Settings
{
    public class BackupsSettings
    {
        public static string BackupsFolderPath = Path.Combine(AssistSettings.SettingsFolderPath, "Backups");
        public BackupsSettings()
        {
            Directory.CreateDirectory(BackupsFolderPath);
        }

        public static void SaveBackup(BackupModel model)
        {
            string[] folders = System.IO.Directory.GetDirectories(BackupsFolderPath,"*", System.IO.SearchOption.AllDirectories);

            var p = Path.Combine(BackupsFolderPath, model.PlayerUuid);
            var configBackupPath = Path.Combine(p, "Config");
            var dataBackupPath = Path.Combine(p, "Data");

            Directory.CreateDirectory(p);
            Directory.CreateDirectory(configBackupPath);
            Directory.CreateDirectory(dataBackupPath);

            CopyFiles(model.ConfigFolderPath, configBackupPath); // Copies Riot Config Files to Backups Folder
            CopyFiles(model.DataFolderPath, dataBackupPath); // Copies Riot Data Files to Backups Folder

            if (Directory.GetFiles(configBackupPath).ToList().Contains("lockfile"))
            {
                var path = Directory.GetFiles(configBackupPath).ToList().Find(file => file.Contains("lockfile"));
                File.Delete(path);
            }
            // Copy Content File as well
            var settings = new BackupSettingsContent()
            {
                BackupModelSettings = new BackupModel()
                {
                    PlayerUuid = model.PlayerUuid,
                    ConfigFolderPath = configBackupPath,
                    DataFolderPath = dataBackupPath,
                },
                CreatedAt = DateTime.Now,
                IsUsed = false
            };


            var content = JsonSerializer.Serialize(settings);
            File.WriteAllText(Path.Combine(p, "Settings.json"), content);
        }

        private static void CopyFiles(string sDir, string nDir)
        {
            foreach(var file in Directory.GetFiles(sDir))
                File.Copy(file, Path.Combine(nDir, Path.GetFileName(file)), true);
        }

        public static async Task<bool> CheckIfBackupExistsForId(string id)
        {
            string[] folders = System.IO.Directory.GetDirectories(BackupsFolderPath, "*", System.IO.SearchOption.TopDirectoryOnly);
            var f = folders.ToList().Find(folder => folder.Contains(id));

            if (string.IsNullOrEmpty(f))
                return false;

            return true;
        }

        public static async Task<ProfileSettings> ReadBackupFromId(string id)
        {
            string[] folders = System.IO.Directory.GetDirectories(BackupsFolderPath, "*", System.IO.SearchOption.TopDirectoryOnly);
            var f = folders.ToList().Find(folder => folder.Contains(id));

            if (f == null)
                throw new Exception("Attempted to Load Backup Config from an ID that does not exist.");

            var files = Directory.GetFiles(f);
            

            // Check if the backup has been used before.
            string? settingsFilePath = files.Single(file => file.Contains("Settings.json"));

            if (string.IsNullOrEmpty(settingsFilePath))
            {
                throw new Exception("No Backup Settings were found.");
            }

            var settingsFile = JsonSerializer.Deserialize<BackupSettingsContent>(File.ReadAllText(settingsFilePath));

            if (settingsFile.IsUsed)
            {
                throw new Exception("Backup has been used.");
            }



            var filePath = Directory.GetFiles(settingsFile.BackupModelSettings.DataFolderPath).Single(file => file.Contains("RiotGamesPrivateSettings"));
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var settings = deserializer.Deserialize<RiotGamesPrivateModel>(File.ReadAllText(filePath));

            if (!await CheckSettings(settings))
            {
                Log.Information("Bad backup");

                throw new Exception("Bad Backup File");
            }

            var dic = await RCAuthViewModel.CreateCookieDic(settings);

            Log.Information("Backup is read and is good to go");
            var pSettings = new ProfileSettings();
            pSettings.ConvertCookiesTo64(dic);


            settingsFile.IsUsed = true;



            File.WriteAllText(Path.Combine(f, "Settings.json"), JsonSerializer.Serialize(settingsFile)); // Apply change to file.

            return pSettings;
        }

        public static void LoadBackupConfigFromId(string id)
        {
            string[] folders = System.IO.Directory.GetDirectories(BackupsFolderPath, "*", System.IO.SearchOption.AllDirectories);
            var f = folders.ToList().Find(folder => folder == id);

            if (f == null)
                throw new Exception("Attempted to Load Backup Config from an ID that does not exist.");


            var p = Path.Combine(BackupsFolderPath, id);

            var settings = JsonSerializer.Deserialize<BackupSettingsContent>(Path.Combine(p, "Settings.json"));

            

        }

        private static async Task<bool> CheckSettings(RiotGamesPrivateModel config)
        {
            try
            {
                if (config.RiotLogin?.Persist?.Session?.Cookies != null)
                {
                    var ssid = config.RiotLogin.Persist.Session.Cookies.Find(c => c.name == "ssid");

                    if (ssid != null)
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            return false;
        }
    }

    public class BackupSettingsContent
    {
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public BackupModel BackupModelSettings { get; set; }
    }
}