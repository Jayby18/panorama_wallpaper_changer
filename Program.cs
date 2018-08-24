﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Security.Permissions;
using Microsoft.Win32;

namespace panorama_wallpaper_changer
{
    class Program
    {
        //Saved in save file
        public string currentVersion = @"1.1.2";
        public int wallpaperAmount;
        public string steamInstallPath; //Folder containing steam.exe
        public string csgoInstallPath; //Folder containing csgo.exe
        public string panoramaWallpaperStoragePath; //Folder with all 'inactive' pano wallpapers
        public string panoramaWallpaperPath; //Folder with 'active' pano wallpapers and the folder above
        public string activeWallpaper = @"n/a"; //Wallpaper that will be replaced by selected wallpaper
        public bool revealChosenWallpaper = false;

        //Internal code variables
        public string selectedWallpaper; //Wallpaper that will replace active wallpaper
        public string saveFile = "C:\\ProgramData\\Panorama Wallpaper Changer\\saveddata.txt"; 
        public string[] wallpapers;
        string[] steamLibraries = new string[32];

        static void Main(string[] args)
        {
            Program pr = new Program();

            Log(" ", false);
            Log("New Session");

            Console.Clear();
            pr.Start();
        }

        void Start()
        {
            string saveFileVersion;
            //First check if a panorama wallpaper changer save file is found
            if (File.Exists(saveFile))
            {
                //Read save file and set variables
                using (StreamReader sr = File.OpenText(saveFile))
                {
                    saveFileVersion = sr.ReadLine();
                    wallpaperAmount = int.Parse(sr.ReadLine());
                    steamInstallPath = sr.ReadLine();
                    csgoInstallPath = sr.ReadLine();
                    panoramaWallpaperPath = sr.ReadLine();
                    panoramaWallpaperStoragePath = sr.ReadLine();
                    activeWallpaper = sr.ReadLine();
                    revealChosenWallpaper = bool.Parse(sr.ReadLine());

                    try
                    {
                        wallpapers = Directory.GetDirectories(panoramaWallpaperStoragePath);
                    } catch (ArgumentException) {
                        //If path is empty, go to setup
                        Setup();
                    }
                }

                string[] currentVersionSplit = currentVersion.Split('.');
                string[] saveFileVersionSplit = saveFileVersion.Split('.');

                if (currentVersionSplit[0] != saveFileVersionSplit[0])
                {
                    //New version is not backwards compatible, so user will need to go through setup again
                    Setup();
                } else {
                    //If amount of wallpapers from save file and actual amount is not equal, change it
                    if (wallpaperAmount != wallpapers.Length)
                    {
                        wallpaperAmount = wallpapers.Length;      
                    }
                    using (StreamWriter sw = File.CreateText(saveFile))
                    {
                        sw.WriteLine(currentVersion);
                        sw.WriteLine(wallpaperAmount);
                        sw.WriteLine(steamInstallPath);
                        sw.WriteLine(csgoInstallPath);
                        sw.WriteLine(panoramaWallpaperPath);
                        sw.WriteLine(panoramaWallpaperStoragePath);
                        sw.WriteLine(activeWallpaper);
                        sw.WriteLine(revealChosenWallpaper);
                    }

                    ChooseWallpaper();
                }     
            } else {
                //Save file was not found, so user will now do setup
                Setup();
            }
        }

        public void Setup()
        {
            string userInput;

            Log("No save file was found. Proceeding with setup.");

            //Find Steam install path in registry (thank you u/DontRushB)
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
            {
                if (key != null)
                {
                    steamInstallPath = key.GetValue("InstallPath").ToString();
                    Log(@"RegistryKey for Steam installation was found at 'SOFTWARE\WOW6432Node\Valve\Steam'.");
                } else {
                    using (RegistryKey key2 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                    {
                        if (key != null)
                        {
                            steamInstallPath = key.GetValue("InstallPath").ToString();
                            Log(@"RegistryKey for Steam installation was found at 'SOFTWARE\Valve\Steam'.");
                        } else {
                            Log("No Steam installation RegistryKey was found. Prompting user to manually enter it...");
                            Console.WriteLine("Steam Install Path was not found, please enter it below:");
                            steamInstallPath = Console.ReadLine();
                        }
                    }
                }
            }

            //Look through Steam libraries until CSGO is found
            if (File.Exists(steamInstallPath + "\\steamapps\\common\\Counter-Strike Global Offensive\\csgo.exe"))
            {
                Log("CSGO was found inside default Steam library.");
                csgoInstallPath = steamInstallPath + "\\steamapps\\common\\Counter-Strike Global Offensive\\";
            } else {
                Log("CSGO was not found inside default Steam library. Searching other libraries now...");
                string[] fileInput = File.ReadAllLines(steamInstallPath + @"\steamapps\libraryfolders.vdf");
                Console.WriteLine(fileInput);
                Console.WriteLine(fileInput.Length);

                for (int n = 0; n < (fileInput.Length); n++)
                {
                    steamLibraries[n] = fileInput[n];
                }

                for (int n = 0; n < steamLibraries.Length; n++)
                {
                    try
                    {
                        if (steamLibraries[n].Length > 4)
                        {
                            steamLibraries[n] = steamLibraries[n].Trim();
                            steamLibraries[n] = steamLibraries[n].Remove(0, 3);
                            steamLibraries[n] = steamLibraries[n].Trim();
                            steamLibraries[n] = steamLibraries[n].Trim( new char[] { '"' } );
                            steamLibraries[n] = steamLibraries[n].Trim();
                            if (steamLibraries[n].EndsWith("SteamLibrary"))
                            {
                                Log(String.Format("Steam Library found at {0}", steamLibraries[n]));
                                Console.WriteLine("Steam Library found at {0}", steamLibraries[n]);
                            } else {
                                steamLibraries[n] = null;
                            }
                        }
                    } catch (NullReferenceException) { }
                }

                for (int n = 0; n < steamLibraries.Length; n++)
                {
                    string csgoSearchPath = steamLibraries[n] + "\\steamapps\\common\\Counter-Strike Global Offensive\\";
                    if (File.Exists(csgoSearchPath + "csgo.exe"))
                    {
                        Log("CSGO found!");
                        Console.WriteLine("CSGO found!");
                        csgoInstallPath = csgoSearchPath;
                        break;
                    }
                }
            }

            panoramaWallpaperPath = csgoInstallPath + "csgo\\panorama\\videos\\";
            panoramaWallpaperStoragePath = panoramaWallpaperPath + "stored\\";

            if (!Directory.Exists("C:\\ProgramData\\Panorama Wallpaper Changer\\"))
            {
                //If save file directory doesn't exist, create it
                Directory.CreateDirectory("C:\\ProgramData\\Panorama Wallpaper Changer\\");
            }

            //Write data to savefile
            using (StreamWriter sw = File.CreateText(saveFile))
            {
                sw.WriteLine(currentVersion);
                sw.WriteLine(wallpaperAmount);
                sw.WriteLine(steamInstallPath);
                sw.WriteLine(csgoInstallPath);
                sw.WriteLine(panoramaWallpaperPath);
                sw.WriteLine(panoramaWallpaperStoragePath);
                sw.WriteLine(activeWallpaper);
                sw.WriteLine(revealChosenWallpaper);
            }

            Log("Setup complete.");
            Console.WriteLine("Setup complete.");
            Console.WriteLine("Press any button to continue...");
            Console.ReadKey();
            Console.Clear();
            Start();
        }

        public void ChooseWallpaper()
        {
            while (true)
            {
                //Get a random number within range of wallpapers
                Random r = new Random();
                int i = r.Next(0, wallpaperAmount);

                selectedWallpaper = wallpapers[i];
                Log(String.Format("Selected wallpaper: {0}.", selectedWallpaper));
                if (selectedWallpaper != activeWallpaper && selectedWallpaper != panoramaWallpaperStoragePath + "backup" 
                    && File.Exists(selectedWallpaper + "\\nuke.webm") && File.Exists(selectedWallpaper + "\\nuke540p.webm") 
                    && File.Exists(selectedWallpaper + "\\nuke720p.webm"))
                {
                    SetWallpaper(true);
                    break;
                } else {
                    Log(("Selected wallpaper cannot be used."));
                }
            }
        }

        public void SetWallpaper(bool runCS)
        {
            //Replace active wallpaper with new wallpaper
            File.Copy(selectedWallpaper + "\\nuke.webm", panoramaWallpaperPath + "\\nuke.webm", true);
            File.Copy(selectedWallpaper + "\\nuke540p.webm", panoramaWallpaperPath + "\\nuke540p.webm", true);
            File.Copy(selectedWallpaper + "\\nuke720p.webm", panoramaWallpaperPath + "\\nuke720p.webm", true);
            
            //Update activeWallpaper here and in save file
            activeWallpaper = selectedWallpaper;
            using (StreamWriter sw = File.CreateText(saveFile))
            {
                sw.WriteLine(currentVersion);
                sw.WriteLine(wallpaperAmount);
                sw.WriteLine(steamInstallPath);
                sw.WriteLine(csgoInstallPath);
                sw.WriteLine(panoramaWallpaperPath);
                sw.WriteLine(panoramaWallpaperStoragePath);
                sw.WriteLine(activeWallpaper);
                sw.WriteLine(revealChosenWallpaper);
            }

            if (revealChosenWallpaper)
            {
                Console.WriteLine("Wallpaper set to " + activeWallpaper);
                Console.ReadKey();
            }

            Log(String.Format("Wallpaper set to: {0}.", activeWallpaper));

            if (runCS) 
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(steamInstallPath + "\\Steam.exe");
                startInfo.Arguments = "-applaunch 730";
                Process.Start(startInfo);
            }
        }

        public static void Log(string logMessage, bool outputDateTime = true)
        {
            string outputPath = @"C:\ProgramData\Panorama Wallpaper Changer\log.txt";
            string outputText;

            using (StreamWriter sw = new StreamWriter(outputPath, true))
            {
                if (outputDateTime)
                {
                    sw.WriteLine(logMessage + String.Format("    [{0}]", DateTime.Now.ToString()));
                } else {
                    sw.WriteLine(logMessage);
                }
            }
        }
    }
}
