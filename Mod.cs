using System.Text.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;


public enum TimestampType
{
    Created,
    Modified
}


public enum Validity
{
    Valid,
    OutOfDate,
    NoModFile,
    NoModioJson,
    NoClientModFile,
    VagueModFile
}


public class Mod
{
    private String                          _modPath;
    private long                            _modTimeUpdated;
    private JsonDocument?                   _modJson;
    private IDictionary<string, FileInfo>   _modFiles;
    

    public String? ModFileDownloadUrl => this._GetModProperty("modfile", "download", "binary_url");
    public String? Name => this._GetModProperty("name");
    public String? Id => this._GetModProperty("id");
    public String  Path => this._modPath;
    public Boolean InstanceIsValid;


    private JsonDocument? _GetModJson()
    {
        if (this._modFiles.ContainsKey("modio.json"))
        {
            try
            {
                return JsonDocument.Parse(System.IO.File.ReadAllText(_modFiles["modio.json"].FullName));
            }
            catch (System.Exception)
            {
                /* Exception handling is for nerds */
                return null;
            }
        }

        return null;
    }


    private String? _GetModProperty(params string[] keys)
    {
        List<string> keysList = new List<string>(keys);
        JsonElement modProperty;
        
        if (this._modJson is null)
        {
            return null;
        }


        if (keysList.Count > 1)
        {
            if (_modJson.RootElement.TryGetProperty(keysList[0], out modProperty) == false)
            {
                return null;
            }
            else
            {
                keysList.RemoveAt(0);
                foreach (string key in keysList)
                {
                    if (modProperty.TryGetProperty(key, out modProperty) == false)
                    {
                        return null;
                    }
                }
            }
        }
        else
        {
            if (_modJson.RootElement.TryGetProperty(keys[0], out modProperty) == false)
            {
                return null;
            }
        }


        return modProperty.ToString();
    }


    private long _StatFileDate(FileInfo fileInfo, TimestampType timestamp_type)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo("/bin/stat")
            {
                ArgumentList = {
                    "-c",
                    timestamp_type == TimestampType.Created ? "%W" : "%Y",
                    fileInfo.FullName
                }
            };


            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;

            process.StartInfo = startInfo;
            process.Start();

            String output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            long fileTimeCreated;
            if (long.TryParse(output, out fileTimeCreated))
            {
                return fileTimeCreated;
            }
            else
            {
                return -1;
            }
        }
        else
        {
            if (timestamp_type == TimestampType.Created)
            {
                return ((DateTimeOffset) fileInfo.CreationTimeUtc).ToUnixTimeSeconds(); 
            }
            else
            {
                return ((DateTimeOffset) fileInfo.LastWriteTimeUtc).ToUnixTimeSeconds();
            }
        }
    }


    private long _GetFileCreationTime(FileInfo fileInfo)
    {
        return _StatFileDate(fileInfo, TimestampType.Created);
    }


    private long _GetFileModificationTime(FileInfo fileInfo)
    {
        return _StatFileDate(fileInfo, TimestampType.Modified);
    }


    private Boolean _IsFileOutOfDate(FileInfo fileInfo)
    {
        long fileTimeCreated  = _GetFileCreationTime(fileInfo);
        long fileTimeModified = _GetFileModificationTime(fileInfo);
        return (fileTimeCreated < _modTimeUpdated && fileTimeModified < _modTimeUpdated);
    }


    private Boolean _ContainsPak()
    {
        foreach (string file in _modFiles.Keys)
        {
            if (file.EndsWith(".pak"))
            {
                return true;
            }
        }

        return false;
    }


    private Boolean _ContainsWindowsClientPak()
    {
        foreach (string file in _modFiles.Keys)
        {
            if (file.EndsWith("WindowsClient.pak"))
            {
                return true;
            }
        }

        return false;
    }


    private Boolean _ContainsServerPak()
    {
        foreach (string file in _modFiles.Keys)
        {
            if (file.EndsWith("Server.pak"))
            {
                return true;
            }
        }

        return false;
    }


    public void DeleteModFiles()
    {
        foreach (string file in _modFiles.Keys)
        {
            if (file.EndsWith(".pak"))
            {
                _modFiles[file].Delete();
            }
        }
    }


    public Validity ModInstallationIsValid()
    {
        // Bare minimum check for validity. We can't actually do
        // anything if there's no _modJson, since we pull all our
        // mod information from that, but nevertheless the installation is
        // still invalid without one.
        if (_modJson is null || this._ContainsPak() == false)
        {
            return Validity.NoModFile;
        }


        if (_ContainsWindowsClientPak() == false)
        {
            if (_ContainsServerPak() == false)
            {
                return Validity.VagueModFile;
            }
            else
            {
                // It's still possible that the client pak is there but
                // does not have the WindowsClient.pak ending, but that is
                // absolutely absurd to do, so let's just assume it's broken instead.
                return Validity.NoClientModFile;
            }
        }


        foreach (String fileInfo in _modFiles.Keys)
        {
            if (_IsFileOutOfDate(_modFiles[fileInfo]) && fileInfo.EndsWith(".pak"))
            {
                return Validity.OutOfDate;
            }
        }

        return Validity.Valid;
    }


    public Mod(String modDirPath)
    {
        this._modPath = modDirPath;
        this._modFiles = new Dictionary<String, FileInfo>();

        foreach (string file in System.IO.Directory.GetFiles(this._modPath))
        {
            this._modFiles[System.IO.Path.GetFileName(file)] = new FileInfo(file);
        }

        this._modJson = this._GetModJson();
        InstanceIsValid = (this._modJson is not null 
                           && long.TryParse(_GetModProperty("modfile", "date_added"), out this._modTimeUpdated));

    }
}