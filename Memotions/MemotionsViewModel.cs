using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Newtonsoft.Json.Linq;
using Plugin.Media;
using Plugin.Media.Abstractions;

namespace Memotions
{
	public class MemotionsViewModel : INotifyPropertyChanged
	{
		string bingSearchKey = "<Insert Your Bing Key>";
		string emotionServiceKey = "<Insert Your Emotions Key>";

		bool _isMyPhotoBusy;

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this,
					new PropertyChangedEventArgs(propertyName));
			}
		}

		string _myPhotoSource;

		public string MyPhotoSource
		{
			get
			{
				return _myPhotoSource;
			}
			set
			{
				_myPhotoSource = value;
				OnPropertyChanged("MyPhotoSource");
			}
		}



		Uri _matchedPhotoSource;

		public Uri MatchedPhotoSource
		{
			get
			{
				return _matchedPhotoSource;
			}
			set
			{
				_matchedPhotoSource = value;
				OnPropertyChanged("MatchedPhotoSource");
			}
		}

		public bool IsMyPhotoBusy
		{
			get
			{
				return _isMyPhotoBusy;
			}
			set
			{
				_isMyPhotoBusy = value;
				OnPropertyChanged("IsMyPhotoBusy");
			}
		}


		bool _isMatchingImageBusy;

		public bool IsMatchingImageBusy
		{
			get
			{
				return _isMatchingImageBusy;
			}
			set
			{
				_isMatchingImageBusy = value;
				OnPropertyChanged("IsMatchingImageBusy");
			}
		}

		public string SearchTerm { get; set; } = "Aubergine";

		public async Task ShowMatch()
		{

			MyPhotoSource = null;
			MatchedPhotoSource = null;

			IsMyPhotoBusy = true;
			IsMatchingImageBusy = true;
			var file = await TakePhoto();

			if (file == null)
			{
				IsMyPhotoBusy = false;
				IsMatchingImageBusy = false;
				return;
			}

			MyPhotoSource = file.Path;
			IsMyPhotoBusy = false;

			var emotion = await GetPhotoEmotion(file);

			if (emotion == null)
			{
				IsMatchingImageBusy = false;
				return;
			}

			string imageUrl = await GetEmotionalSearchResult(emotion, SearchTerm);

			if (imageUrl == null)
			{
				IsMatchingImageBusy = false;
				return;
			}

			MatchedPhotoSource = new Uri(imageUrl);
			IsMatchingImageBusy = false;

		}

		async Task<string> GetPhotoEmotion(MediaFile file)
		{
			var imageStream = file.GetStream();

			var emotionServiceClient = new EmotionServiceClient(emotionServiceKey);

			Emotion[] emotionResult = await emotionServiceClient.RecognizeAsync(imageStream);

			if (!emotionResult.Any())
				return null;

			var faceEmotion = emotionResult[0]?.Scores;

			var dictionary = new Dictionary<string, double>();

			dictionary.Add("happy", faceEmotion.Happiness);
			dictionary.Add("angry", faceEmotion.Anger);
			dictionary.Add("sad", faceEmotion.Sadness);
			dictionary.Add("disgusted", faceEmotion.Disgust);
			dictionary.Add("contemptuous", faceEmotion.Contempt);
			dictionary.Add("suprised", faceEmotion.Surprise);
			dictionary.Add("scared", faceEmotion.Fear);
			dictionary.Add("neutral", faceEmotion.Neutral);

			var search = dictionary.OrderByDescending((KeyValuePair<string, double> arg) => arg.Value).Select(x => x.Key).First();

			return search;
		}

		async Task<MediaFile> TakePhoto()
		{
			await CrossMedia.Current.Initialize();

			if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
			{
				return null;
			}

			var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
			{
				Directory = "Sample",
				Name = "test.jpg"
			});

			return file;
		}


		async Task<string> GetEmotionalSearchResult(string emotion, string searchTerm)
		{
			string fullTerm = emotion + " " + searchTerm;

			var client = new HttpClient();

			client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", bingSearchKey);
			var uri = string.Format("https://api.cognitive.microsoft.com/bing/v5.0/images/search?q={0}&count=1&safesearch=Moderate", fullTerm);

			var response = await client.GetAsync(uri);
			var json = JObject.Parse(await response.Content.ReadAsStringAsync());

			JToken firstValue = json["value"];

			if (!firstValue.Any())
				return null;

			JToken firstResult = firstValue[0];
			JToken url = firstResult["hostPageDisplayUrl"];
			JToken contentUrl = firstResult["contentUrl"];
			return contentUrl.ToString();


		}
	}
}
