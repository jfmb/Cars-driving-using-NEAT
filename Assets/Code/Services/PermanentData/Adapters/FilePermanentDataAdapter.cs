using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class FilePermanentDataAdapter : IPermanentData
{
    private const int CurrentFileVersion = 1;
    internal const string DefaultFileName = "gameInfo.txt";

    private readonly string _directoryPath;
    private readonly string _filePath;
    private readonly string _backupPath;
    private readonly Dictionary<string, string> _stringData = new();
    private readonly Dictionary<string, int> _integerData = new();

    private bool _canPersist = true;
    private bool _writeFailureReported;

    public FilePermanentDataAdapter(string fileName = DefaultFileName)
    {
        _directoryPath = Application.persistentDataPath;
        _filePath = Path.Combine(_directoryPath, GetSafeFileName(fileName));
        _backupPath = _filePath + ".bak";

        LoadFromDisk();
    }

    public void LoadSingleData(string key, out string data)
    {
        ValidateKey(key);
        _stringData.TryGetValue(key, out data);
    }

    private static void ValidateKey(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }
    }

    public void LoadSingleData(string key, out int data)
    {
        ValidateKey(key);
        _integerData.TryGetValue(key, out data);
    }

    public void SaveSingleData(string key, string data)
    {
        ValidateKey(key);
        if (_stringData.TryGetValue(key, out var currentValue) && currentValue == data)
        {
            return;
        }

        _stringData[key] = data;
        Persist();
    }

    private bool Persist()
    {
        if (!_canPersist)
        {
            if (!_writeFailureReported)
            {
                Debug.LogError("FilePermanentDataAdapter: persistent storage is unavailable; " +
                               "the existing file was left untouched.");
                _writeFailureReported = true;
            }

            return false;
        }

        var temporaryPath = _filePath + ".tmp";
        try
        {
            Directory.CreateDirectory(_directoryPath);
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(CreateFileData(), true), new UTF8Encoding(false));

            if (File.Exists(_filePath))
            {
                File.Copy(_filePath, _backupPath, true);
                File.Delete(_filePath);
            }

            File.Move(temporaryPath, _filePath);
            try
            {
                File.Copy(_filePath, _backupPath, true);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"FilePermanentDataAdapter: saved '{_filePath}', but could not refresh " +
                                 $"its recovery copy. {exception}");
            }

            _writeFailureReported = false;
            return true;
        }
        catch (Exception exception)
        {
            RestoreAfterFailedWrite();
            Debug.LogError($"FilePermanentDataAdapter: could not save '{_filePath}'. {exception}");
            _writeFailureReported = true;
            return false;
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private FileData CreateFileData()
    {
        var data = new FileData
        {
            formatVersion = CurrentFileVersion,
            strings = new List<StringEntry>(),
            integers = new List<IntegerEntry>()
        };
        foreach (var item in _stringData)
        {
            data.strings.Add(new StringEntry { key = item.Key, value = item.Value });
        }

        foreach (var item in _integerData)
        {
            data.integers.Add(new IntegerEntry { key = item.Key, value = item.Value });
        }

        return data;
    }

    private void RestoreAfterFailedWrite()
    {
        if (File.Exists(_filePath) || !File.Exists(_backupPath))
        {
            return;
        }

        try
        {
            File.Copy(_backupPath, _filePath, false);
        }
        catch (Exception exception)
        {
            Debug.LogError($"FilePermanentDataAdapter: could not restore backup '{_backupPath}'. {exception}");
        }
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"FilePermanentDataAdapter: could not remove temporary file '{path}'. {exception}");
        }
    }

    public void SaveSingleData(string key, int data)
    {
        ValidateKey(key);
        if (_integerData.TryGetValue(key, out var currentValue) && currentValue == data)
        {
            return;
        }

        _integerData[key] = data;
        Persist();
    }

    public void LoadGroupOfData(Dictionary<string, string> data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        var keys = new List<string>(data.Keys);
        foreach (var key in keys)
        {
            ValidateKey(key);
            if (_stringData.TryGetValue(key, out var value))
            {
                data[key] = value;
            }
        }
    }

    public void LoadGroupOfData(Dictionary<string, int> data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        var keys = new List<string>(data.Keys);
        foreach (var key in keys)
        {
            ValidateKey(key);
            if (_integerData.TryGetValue(key, out var value))
            {
                data[key] = value;
            }
        }
    }

    public void SaveGroupOfData(Dictionary<string, string> data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        ValidateKeys(data.Keys);
        var changed = false;
        foreach (var item in data)
        {
            if (_stringData.TryGetValue(item.Key, out var currentValue) && currentValue == item.Value)
            {
                continue;
            }

            _stringData[item.Key] = item.Value;
            changed = true;
        }

        if (changed)
        {
            Persist();
        }
    }

    private static void ValidateKeys(IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            ValidateKey(key);
        }
    }

    public void SaveGroupOfData(Dictionary<string, int> data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        ValidateKeys(data.Keys);
        var changed = false;
        foreach (var item in data)
        {
            if (_integerData.TryGetValue(item.Key, out var currentValue) && currentValue == item.Value)
            {
                continue;
            }

            _integerData[item.Key] = item.Value;
            changed = true;
        }

        if (changed)
        {
            Persist();
        }
    }

    public void DeleteSingleData(string key)
    {
        ValidateKey(key);
        var changed = _stringData.Remove(key);
        changed |= _integerData.Remove(key);
        if (changed)
        {
            Persist();
        }
    }

    public void DeleteGroupOfData(List<string> keys)
    {
        if (keys == null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        ValidateKeys(keys);
        var changed = false;
        foreach (var key in keys)
        {
            changed |= _stringData.Remove(key);
            changed |= _integerData.Remove(key);
        }

        if (changed)
        {
            Persist();
        }
    }

    public void DeleteAllData()
    {
        if (_stringData.Count > 0 || _integerData.Count > 0)
        {
            _stringData.Clear();
            _integerData.Clear();

            if (!Persist())
            {
                return;
            }
        }

        try
        {
            if (File.Exists(_backupPath))
            {
                File.Delete(_backupPath);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"FilePermanentDataAdapter: settings were cleared, but backup " +
                           $"'{_backupPath}' could not be removed. {exception}");
        }
    }

    private static string GetSafeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return DefaultFileName;
        }

        if (fileName == "." || fileName == ".." || Path.IsPathRooted(fileName) ||
            fileName.IndexOf('/') >= 0 || fileName.IndexOf('\\') >= 0 || Path.GetFileName(fileName) != fileName)
        {
            Debug.LogError($"FilePermanentDataAdapter: '{fileName}' must be a file name. " +
                           $"Using '{DefaultFileName}'.");
            return DefaultFileName;
        }

        return fileName;
    }

    private void LoadFromDisk()
    {
        try
        {
            Directory.CreateDirectory(_directoryPath);
        }
        catch (Exception exception)
        {
            DisablePersistence($"could not access persistent data directory '{_directoryPath}'", exception);
            return;
        }

        if (File.Exists(_filePath))
        {
            if (TryReadFile(_filePath, out var data))
            {
                ApplyData(data);
                return;
            }

            if (!TryPreserveFile(_filePath))
            {
                _canPersist = false;
                return;
            }

            if (TryRestoreBackup())
            {
                return;
            }

            if (File.Exists(_backupPath) && !TryPreserveFile(_backupPath))
            {
                _canPersist = false;
                return;
            }

            Persist();
            return;
        }

        if (File.Exists(_backupPath))
        {
            if (TryReadFile(_backupPath, out var backupData))
            {
                ApplyData(backupData);
                try
                {
                    File.Copy(_backupPath, _filePath);
                    Debug.LogWarning("FilePermanentDataAdapter: restored gameInfo.txt from its backup.");
                    return;
                }
                catch (Exception exception)
                {
                    DisablePersistence($"could not restore '{_filePath}' from '{_backupPath}'", exception);
                    return;
                }
            }

            if (!TryPreserveFile(_backupPath))
            {
                _canPersist = false;
                return;
            }
        }

        Persist();
    }

    private void DisablePersistence(string reason, Exception exception)
    {
        _canPersist = false;
        Debug.LogError($"FilePermanentDataAdapter: {reason}. {exception}");
    }

    private bool TryReadFile(string path, out FileData data)
    {
        data = null;
        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            var loadedData = JsonUtility.FromJson<FileData>(json);
            ValidateFileData(loadedData);
            data = loadedData;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"FilePermanentDataAdapter: could not read '{path}'. {exception.Message}");
            return false;
        }
    }

    private static void ValidateFileData(FileData data)
    {
        if (data == null || data.formatVersion != CurrentFileVersion || data.strings == null || data.integers == null)
        {
            throw new FormatException("Unsupported or incomplete game data format.");
        }

        var stringKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in data.strings)
        {
            if (entry == null || entry.key == null || !stringKeys.Add(entry.key))
            {
                throw new FormatException("The string data contains a null or duplicate key.");
            }
        }

        var integerKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in data.integers)
        {
            if (entry == null || entry.key == null || !integerKeys.Add(entry.key))
            {
                throw new FormatException("The integer data contains a null or duplicate key.");
            }
        }
    }

    private void ApplyData(FileData data)
    {
        _stringData.Clear();
        _integerData.Clear();

        foreach (var entry in data.strings)
        {
            _stringData.Add(entry.key, entry.value);
        }

        foreach (var entry in data.integers)
        {
            _integerData.Add(entry.key, entry.value);
        }
    }

    private bool TryPreserveFile(string path)
    {
        var preservedPath = path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        try
        {
            File.Copy(path, preservedPath, false);
            Debug.LogWarning($"FilePermanentDataAdapter: preserved '{path}' as '{preservedPath}'.");
            return true;
        }
        catch (Exception exception)
        {
            DisablePersistence($"could not preserve '{path}', so it was left untouched", exception);
            return false;
        }
    }

    private bool TryRestoreBackup()
    {
        if (!File.Exists(_backupPath) || !TryReadFile(_backupPath, out var backupData))
        {
            return false;
        }

        ApplyData(backupData);
        try
        {
            File.Copy(_backupPath, _filePath, true);
            Debug.LogWarning("FilePermanentDataAdapter: recovered gameInfo.txt from its backup.");
        }
        catch (Exception exception)
        {
            DisablePersistence($"data was read from '{_backupPath}' but could not be restored to '{_filePath}'",
                exception);
        }

        return true;
    }

    [Serializable]
    private sealed class FileData
    {
        public int formatVersion;
        public List<StringEntry> strings;
        public List<IntegerEntry> integers;
    }

    [Serializable]
    private sealed class StringEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    private sealed class IntegerEntry
    {
        public string key;
        public int value;
    }
}
