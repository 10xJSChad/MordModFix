using System.Reflection;


public class MordModFix
{
    const string CONFIG_FILENAME = "game-path.txt";
    const string LINUX_CONFIG_FILENAME = "mordmodfix";


    static String ModsPath = ""; // Should be nullable but that throws warnings I cba handling
                                 // at no point in its use can this be an invalid path, so it's fine.
                                 
    static String[] ModDirs(string path) => System.IO.Directory.GetDirectories(ModsPath);


    static String GetModValidityMessage(Validity validity)
    {
        switch (validity)
        {
            case Validity.Valid:
                return ""; // No trailing validity message wanted

            case Validity.OutOfDate:
                return "- out of date";
            
            case Validity.NoModFile:
                return "- missing mod file";
            
            case Validity.NoClientModFile:
                return "- missing client mod file";

            case Validity.VagueModFile:
                return "- non-standard mod file name";

            default:
                return "- unknown error";
        }
    }


    static Validity GetAndPrintModValidity(Mod mod)
    {
        Validity installationValidity;
        
        if (mod.InstanceIsValid == false)
        {
            return Validity.NoModioJson;
        }
        
        installationValidity = mod.ModInstallationIsValid();
        switch (installationValidity)
        {
            case Validity.Valid:
                Console.ForegroundColor = ConsoleColor.Green;
                break;

            case Validity.VagueModFile:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            
            default:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
        }

        Console.Write("({0}) {1} {2}\n", mod.Id, mod.Name, GetModValidityMessage(installationValidity));
        Console.ResetColor();

        return installationValidity;
    }
    

    static async Task ReinstallMods(List<Mod> brokenMods)
    {
        HttpClient httpClient = new HttpClient();
        foreach (Mod mod in brokenMods)
        {
            // TODO:
            // implement actual modio.json checks, for now, maybe just
            // avoid tampering with your modio.json files :)
            if (mod.Id is null)
            {
                continue;
            }

            Console.Write("Re-installing {0}... ", mod.Name);
            mod.DeleteModFiles();

            String path     =  (ModsPath + "/" + mod.Id.ToString());
            String filename =  "MORDMODFIX_TEMP.zip"; 

            var response = await httpClient.GetStreamAsync(mod.ModFileDownloadUrl);
            using (var fileStream = File.Create(path + "/" + filename, 8192))
            {
                await response.CopyToAsync(fileStream);
            }

            System.IO.Compression.ZipFile.ExtractToDirectory(path + "/" + filename, path);
            System.IO.File.Delete(path + "/" + filename);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Success!\n", mod.Name);
            Console.ResetColor();
        }

        Console.WriteLine("\nAll mods have been successfully re-installed!");
    }


    static String GetExecutablePath()
    {
        return System.AppContext.BaseDirectory;
    }


    static String? GetConfig()
    {
        String executablePath = GetExecutablePath();
        String configPath = executablePath + "/" + CONFIG_FILENAME;
        return File.Exists(configPath) ? configPath : null;
    }


    // Alternate config for Linux so you can separate the binary and the config
    static String? GetLinuxConfig()
    {
        String homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        String configDir = homeDir + "/.config";

        if (Directory.Exists(configDir) == false)
        {
            Console.WriteLine("~/.config does not exist");
            return null;
        }

        String configPath = configDir + "/" + LINUX_CONFIG_FILENAME;
        return File.Exists(configPath) ? configPath : null;
    }


    static String? ReadConfig()
    {
        String? config = GetConfig();

        if (config is null)
        {
            if ((config = GetLinuxConfig()) is null)
            {
                Console.WriteLine("Could not find config file");
                return null;
            }
        }

        String content = File.ReadAllText(config);
        return content.TrimEnd('\n');
    }


    static String? GetModsPath()
    {
        String modsPath = ReadConfig() + "/Mordhau/Content/.modio/mods";
        return Directory.Exists(modsPath) ? modsPath : null;
    }


    static bool SetModsPath()
    {
        String? modsPath = GetModsPath();

        if (modsPath is null)
        {
            Console.WriteLine("Could not determine mods path");
            return false;
        }

        ModsPath = modsPath;
        return true;
    }


    static async Task Main()
    { 
        if (!SetModsPath())
        {
            goto end;
        }


        Console.WriteLine("--------------Mods--------------");
        List<Mod> mods = (from string mod in ModDirs(ModsPath)
                           let current_mod = new Mod(mod)
                           where GetAndPrintModValidity(current_mod) != Validity.Valid
                           select current_mod).ToList();


        List<Mod> brokenMods = mods.Where(
            mod => mod.ModInstallationIsValid() != Validity.VagueModFile).ToList();

            
        Console.Write("\n");
        switch (brokenMods.Count)
        {
            case 0:
                Console.WriteLine("There are no mods in need of repair");
                goto end; // cry about it

            case 1:
                Console.WriteLine("Found 1 improperly installed mod, re-install it? (y/N)");
                break;

            default:
                Console.WriteLine("Found {0} improperly installed mods, re-install these? (y/N)", brokenMods.Count);
                break;
        }

        String? input = Console.ReadLine();
        if (input is not null && input.ToLower() == "y")
        {
            Console.Write("\n");
            await ReinstallMods(brokenMods);
        }

end:;
        Console.WriteLine("Press ENTER to exit...");
        Console.ReadLine();
    }
}
