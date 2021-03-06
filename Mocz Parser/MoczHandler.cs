﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Dictionary_Packager.Japanese;

namespace Mocz_Parser
{
    public enum InputMethod
    {
        LocalApi,
        GoogleApi,
        JMDictApi
    }

    public class MoczHandler : INotifyPropertyChanged
    {

        private MoczModel _moczModel;
        public readonly string GoogleAPI = "http://www.google.com/transliterate";

        public ObservableCollection<string> ResultList { get; set; } = new ObservableCollection<string>();

        public MoczHandler()
        {
            _moczModel = new MoczModel();
        }

        public void Init()
        {
            _moczModel.Load();
        }

        public void LoadResults(string input, InputMethod method)
        {
            ResultList.Clear();

            switch (method)
            {
                case InputMethod.GoogleApi:
                    LoadResultsWithGoogleApi(input);
                    return;
                default:
                    LoadResultsWithLocalApi(input);
                    return;
            }
        }

        #region Private Methods

        private void LoadResultsWithLocalApi(string input)
        {
            /* Example of file Structure:
             * 
             * Input Text | leftId | rightId | cost |    Text
             *    あいたた	  2584	     142   5884	  あいたた
             *    
             * First approach select other Parts according to the first id.
             * Then order them ascend to cost.
             */
            var matches = _moczModel.Ipadic.Where(x => x.Hiragana == input);
            matches = matches.GroupBy(x => x.DictionaryEntry).Select(x =>
                new JapaneseDictEntry
                {
                    DictionaryEntry = x.Key,
                    Cost = x.Sum(entry => entry.Cost)
                });
            matches = matches.OrderBy(x => x.Cost);
            foreach (var japaneseDictEntry in matches)
            {
                ResultList.Add(japaneseDictEntry.DictionaryEntry);
            }
            /*
            foreach (var match in matches)
            {
                if (ResultList.Contains(match.DictionaryEntry)) continue;
                ResultList.Add(match.DictionaryEntry);
            }
            */
        }

        private void LoadResultsWithGoogleApi(string input)
        {
            var requestUrl = $"{GoogleAPI}?langpair=ja-Hira%7Cja&text={input}";
            var request = WebRequest.Create(requestUrl);
            
            using(var response = request.GetResponse())
            {
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    var result = sr.ReadToEnd();
                    dynamic d = JObject.Parse($"{{data:{result}}}");

                    dynamic data = d.data;
                    dynamic firstPart = data[0];
                    dynamic list = firstPart[1];

                    foreach(var entry in list)
                    {
                        ResultList.Add(entry.Value);
                    }
                }
            }

        }

        #endregion

        #region Implementations

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }
}
