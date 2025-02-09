using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace VarCo.GoogleSheetsDownloader
{
    [CreateAssetMenu(menuName = "VarCo/Google Sheets Downloader",
        fileName = "New Google Sheets Downloader")]
    public class GoogleSheetDownloader : ScriptableObject
    {
        [SerializeField, TextArea] private string _credentialsPath;
        [SerializeField, TextArea] private string _savePath;
        [SerializeField] private Formatting _formatting;
        [SerializeField] private string _sheetId;
        [SerializeField] private string _sheetName;
        [SerializeField] private string _range = "A1:Z";

        private const string LOG_PREFIX = "<color=#00d5ff>[GoogleSheetDownloader]:</color> ";
        private bool _isBusy;

        [ContextMenu(nameof(Download))]
        internal async void Download()
        {
            if (!ValidateFields())
                return;

            try
            {
                if (_isBusy) return;
                _isBusy = true;

                EditorUtility.DisplayProgressBar("Downloading", "Downloading...", 0.3f);
                IList<IList<object>> rawTable = await DownloadAsync();
                string json = JsonConvert.SerializeObject(rawTable, _formatting);
                EditorUtility.DisplayProgressBar("Downloading", "Writing to file...", 0.6f);
                await File.WriteAllTextAsync(_savePath, json);

                EditorUtility.DisplayProgressBar("Updating translations", "Finish...", 1f);
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _isBusy = false;
            }
        }

        private bool ValidateFields()
        {
            bool result = true;

            if (string.IsNullOrEmpty(_credentialsPath))
            {
                Debug.LogError($"{LOG_PREFIX}Credentials path is empty");
                result = false;
            }

            if (string.IsNullOrEmpty(_sheetId))
            {
                Debug.LogError($"{LOG_PREFIX}Sheet id is empty");
            }

            if (string.IsNullOrEmpty(_sheetName))
            {
                Debug.LogError($"{LOG_PREFIX}Sheet name is empty");
                result = false;
            }

            if (string.IsNullOrEmpty(_range))
            {
                Debug.LogError($"{LOG_PREFIX}Range is empty");
                result = false;
            }

            if (!File.Exists(_credentialsPath))
            {
                Debug.LogError($"{LOG_PREFIX}Credentials file not found");
                result = false;
            }

            try
            {
                string saveDirectory = Path.GetDirectoryName(_savePath);
                if (!Directory.Exists(saveDirectory))
                {
                    Debug.LogError($"{LOG_PREFIX}Save directory not found");
                    result = false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{LOG_PREFIX}Save directory not found. Error: {e.Message}");
                result = false;
            }

            return result;
        }

        private async UniTask<IList<IList<object>>> DownloadAsync()
        {
            GoogleCredential credential;
            await using (FileStream stream = new(_credentialsPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
            }

            var sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential
            });

            string range = $"{_sheetName}!{_range}";

            SpreadsheetsResource.ValuesResource.GetRequest request =
                sheetsService.Spreadsheets.Values.Get(_sheetId, range);

            ValueRange response;

            try
            {
                response = await request.ExecuteAsync().AsUniTask();
            }
            catch (Exception e)
            {
                Debug.LogError($"{LOG_PREFIX}Error: {e.Message}");
                return null;
            }

            if (!IsEmpty(response))
            {
                return response.Values;
            }

            Debug.LogWarning($"{LOG_PREFIX}Response is empty");
            return null;
        }

        private static bool IsEmpty(ValueRange response)
        {
            return response?.Values == null || response.Values.Count == 0;
        }
    }
}
