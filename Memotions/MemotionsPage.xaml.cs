using Xamarin.Forms;
using Plugin.Media;
using System;
using System.Collections.Generic;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Plugin.Media.Abstractions;

namespace Memotions
{
	public partial class MemotionsPage : ContentPage
	{

		public MemotionsPage()
		{
			InitializeComponent();

			var vm = new MemotionsViewModel();
			BindingContext = vm;

			TakePhotoButton.Clicked += async (sender, e) => await vm.ShowMatch();
		}




	}
}
