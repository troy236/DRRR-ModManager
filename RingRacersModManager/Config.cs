using System;
using System.IO;
using System.Text.Json.Serialization;

namespace RingRacersModManager;
public class Config {
    public string RingRacersPath { get; set; }
    [JsonIgnore]
    public string RingRacersAddonsPath {
        get {
            if (!string.IsNullOrEmpty(this.RingRacersManualAddonPath)) return this.RingRacersManualAddonPath;
            if (OperatingSystem.IsWindows()) {
                //While this is the default install location, it is also the default location assets/addons/save data (except sounds?) are loaded from
                //On Linux and Mac it is completely normal these are separate but on Windows you don't realise assets/addons/save data HAVE to be here
                //Explains why I could never launch a old version of RR outside the default install path
                if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "RingRacers", "addons"))) {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "RingRacers", "addons");
                }
                else return null;
            }
            else if (OperatingSystem.IsLinux()) {
                if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".var", "app", "org.kartkrew.RingRacers", ".ringracers", "addons"))) {

                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".var", "app", "org.kartkrew.RingRacers", ".ringracers", "addons");
                }
                else if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ringracers", "addons"))) {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ringracers", "addons");
                }
                else return null;
            }
            else if (OperatingSystem.IsMacOS()) {
                if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ringracers", "addons"))) {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ringracers", "addons");
                }
                else return null;
            }
            else return null;
        }
    }
    [JsonIgnore]
    public string RingRacersExecutablePath {
        get {
            if (!string.IsNullOrEmpty(this.RingRacersManualExecutablePath)) return this.RingRacersManualExecutablePath;
            if (OperatingSystem.IsWindows()) {
                if (string.IsNullOrEmpty(this.RingRacersPath)) return null;
                return Path.Combine(this.RingRacersPath, "ringracers.exe");
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) {
                if (this.RingRacersPath == null) return null;
                return Path.Combine(this.RingRacersPath, "ringracers");
            }
            else return null;
        }
    }
    public int Theme { get; set; }
    public bool StartupAppUpdateCheck { get; set; }
    public bool StartupAddonsUpdateCheck { get; set; }
    public string RingRacersManualExecutablePath { get; set; }
    public string RingRacersManualAddonPath { get; set; }
}
