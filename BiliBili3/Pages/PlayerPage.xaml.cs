﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Core;
using Newtonsoft.Json.Linq;
using Windows.Media.Editing;
using BiliBili3.Helper;
using NSDanmaku.Helper;
using BiliBili3.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using SYEngine;
using System.Diagnostics;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace BiliBili3.Pages
{
    enum HeartBeatType
    {
        Start,
        Play,
        End
    }
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class PlayerPage : Page
    {
        public PlayerPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            SYEngine.Core.Initialize();
            MTC.DanmuLoaded += MTC_DanmuLoaded;
            CoreWindow.GetForCurrentThread().KeyDown += PlayerPage_KeyDown;
            if (SettingHelper.Get_BackPlay())
            {
                _systemMediaTransportControls = SystemMediaTransportControls.GetForCurrentView();
                _systemMediaTransportControls.IsPlayEnabled = true;
                _systemMediaTransportControls.IsPauseEnabled = true;
                _systemMediaTransportControls.ButtonPressed += _systemMediaTransportControls_ButtonPressed;
            }
        }
        private void MTC_DanmuLoaded(object sender, NSDanmaku.Controls.Danmaku e)
        {
            if (e != null)
            {
                danmu = e;
            }
        }

        private async void _systemMediaTransportControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        mediaElement.Play();
                    });
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        mediaElement.Pause();
                    });
                    break;
                default:
                    break;
            }
        }

        private void PlayerPage_KeyDown(CoreWindow sender, KeyEventArgs args)
        {

            args.Handled = true;
            switch (args.VirtualKey)
            {
                case Windows.System.VirtualKey.Space:
                    if (mediaElement.CurrentState == MediaElementState.Playing)
                    {
                        mediaElement.Pause();
                    }
                    else
                    {
                        mediaElement.Play();
                    }
                    break;
                case Windows.System.VirtualKey.Left:


                    TimeSpan ts = new TimeSpan(0, 0, Convert.ToInt32(mediaElement.Position.TotalSeconds - 3));
                    mediaElement.Position = ts;
                    Utils.ShowMessageToast(mediaElement.Position.Hours.ToString("00") + ":" + mediaElement.Position.Minutes.ToString("00") + ":" + mediaElement.Position.Seconds.ToString("00"), 3000);
                    break;
                case Windows.System.VirtualKey.Up:
                    mediaElement.Volume += 0.1;
                    Utils.ShowMessageToast("音量:" + mediaElement.Volume.ToString("P"), 3000);
                    break;
                case Windows.System.VirtualKey.Right:
                    TimeSpan ts2 = new TimeSpan(0, 0, Convert.ToInt32(mediaElement.Position.TotalSeconds + 3));
                    mediaElement.Position = ts2;
                    Utils.ShowMessageToast(mediaElement.Position.Hours.ToString("00") + ":" + mediaElement.Position.Minutes.ToString("00") + ":" + mediaElement.Position.Seconds.ToString("00"), 3000);
                    break;
                case Windows.System.VirtualKey.Down:
                    mediaElement.Volume -= 0.1;
                    //mediaElement.Balance -= 0.1;
                    Utils.ShowMessageToast("音量:" + mediaElement.Volume.ToString("P"), 3000);
                    break;
                case Windows.System.VirtualKey.Escape:
                    if (MTC.IsFullWindow)
                    {
                        ApplicationView.GetForCurrentView().ExitFullScreenMode();
                    }
                    break;

                case Windows.System.VirtualKey.F11:
                    if (!MTC.IsFullWindow)
                    {
                        MTC.ToFull();
                        //ApplicationView.GetForCurrentView().TryEnterFullScreenMode();

                    }
                    else
                    {
                        MTC.ExitFull();
                        //ApplicationView.GetForCurrentView().ExitFullScreenMode();

                    }
                    break;
                case Windows.System.VirtualKey.F10:
                    CaptureVideo();
                    break;
                default:
                    break;
            }
        }
        SystemMediaTransportControls _systemMediaTransportControls;
        private DisplayRequest dispRequest = null;//保持屏幕常亮
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            this.Frame.Visibility = Visibility.Visible;
            int flag = 1;
            while (true)
            {
                if (danmu != null)
                {
                    break;
                }
                if (flag >= 100)
                {
                    MessageDialog messageDialog = new MessageDialog("播放组件似乎加载失败了,是否报告开发者？");
                    messageDialog.Commands.Add(new UICommand("确定", (sender) => { LogHelper.WriteLog(new Exception("无法加载播放器")); }));
                    messageDialog.Commands.Add(new UICommand("取消"));
                    await messageDialog.ShowAsync();
                    flag = 1;
                }
                await Task.Delay(100);
                flag++;
            }

            // CheckNetwork();
            //await Task.sp_View(200);
            if (e.NavigationMode == NavigationMode.New)
            {
                object[] obj = e.Parameter as object[];
                var ls = obj[0] as List<PlayerModel>;
                var index = (int)obj[1];

                LoadPlayer(ls, index);
                sp_View.Focus(FocusState.Pointer);
            }

        }
        protected async override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            try
            {
                ClosePLayer();
                //Debug.WriteLine("开始返回");
                this.Frame.Visibility = Visibility.Collapsed;
                mediaElement.Pause();
                int i = 0;
                while (true)
                {
                    i++;
                    if (mediaElement.CurrentState != MediaElementState.Buffering && progress.Visibility == Visibility.Collapsed)
                    {
                        break;
                    }
                    //Debug.WriteLine($"判断{i}次");
                    await Task.Delay(200);
                }
                //Debug.WriteLine($"缓冲完成返回");
                //mediaElement.Stop();
                base.OnNavigatingFrom(e);
            }
            catch (Exception ex)
            {

            }
        }

        private async Task CheckNetwork()
        {
            if (SystemHelper.GetNetWorkType() == NetworkType.Other && SettingHelper.Get_Use4GPlay())
            {
                var md = new MessageDialog("当前使用的是数据网络，是否继续播放？\r\n可到设置中关闭此提醒", "播放询问");
                md.Commands.Add(new UICommand("确定", new UICommandInvokedHandler((e) => { })));
                md.Commands.Add(new UICommand("取消", new UICommandInvokedHandler((e) => { this.Frame.GoBack(); return; })));
                await md.ShowAsync();
            }
        }


        private NSDanmaku.Controls.Danmaku danmu;

        DispatcherTimer timer;
        DispatcherTimer timer_Date;
        List<PlayerModel> playList;
        List<NSDanmaku.Model.DanmakuModel> DanMuPool = null;
        PlayerModel playNow;

        //int _index = 0;
        bool playLocal = false;
        bool LoadDanmu = true;
        int LastPost = 0;
        public async void LoadPlayer(List<PlayerModel> par, int index)
        {


            await Task.Delay(200);
            danmu = MTC.myDanmaku;

            UpdateSetting();
            if (SettingHelper.Get_BackPlay())
            {
                //_mediaPlayer = new MediaPlayer();
                // _systemMediaTransportControls = _mediaPlayer.SystemMediaTransportControls;
                //_mediaPlayer.CommandManager.IsEnabled = true;

                _systemMediaTransportControls = SystemMediaTransportControls.GetForCurrentView();
                _systemMediaTransportControls.IsPlayEnabled = true;
                _systemMediaTransportControls.IsPauseEnabled = true;
                _systemMediaTransportControls.ButtonPressed += _systemMediaTransportControls_ButtonPressed;
            }
            if (timer == null)
            {
                timer = new DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 1);
                timer.Tick += Timer_Tick;
                timer.Start();
            }
            if (timer_Date == null)
            {
                timer_Date = new DispatcherTimer();
                timer_Date.Interval = new TimeSpan(0, 0, 1);
                timer_Date.Tick += Timer_Date_Tick; ;
                timer_Date.Start();
            }
            if (dispRequest == null)
            {
                // 用户观看视频，需要保持屏幕的点亮状态
                dispRequest = new DisplayRequest();
                dispRequest.RequestActive(); // 激活显示请求
            }
            if (SettingHelper.Get_QZHP())
            {
                DisplayInformation.AutoRotationPreferences = (DisplayOrientations)5;
            }

            if (SettingHelper.Get_AutoFull())
            {
                MTC.ToFull();
                //mediaElement.IsFullWindow = true;
            }

            playList = par;
            playNow = playList[index];

            //  btn_HideInfo.Visibility = Visibility.Collapsed;
            //   btn_ShowInfo.Visibility = Visibility.Collapsed;
            mediaElement.AutoPlay = true;

            gv_play.ItemsSource = playList;
            gv_play.SelectedIndex = index;


            //DisplayInformation.AutoRotationPreferences = (DisplayOrientations)5;

        }

        public void ClosePLayer()
        {
            try
            {

                if (dispRequest != null)
                {
                    dispRequest = null;
                }

                SettingHelper.Set_Volume(mediaElement.Volume);
                SettingHelper.Set_Light(MTC.Brightness);
                ApplicationView.GetForCurrentView().ExitFullScreenMode();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
                if (timer != null)
                {
                    timer.Stop();
                    timer = null;
                }
                if (timer_Date != null)
                {
                    timer_Date.Stop();
                    timer_Date = null;
                }
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
                //_mediaPlayer.Source = null;
                //_mediaPlayer = null;

                if (_systemMediaTransportControls != null)
                {
                    _systemMediaTransportControls.DisplayUpdater.ClearAll();
                    _systemMediaTransportControls.IsEnabled = false;
                    _systemMediaTransportControls = null;
                }
                //  mediaElement.Stop();
                //  gv_play.ItemsSource = null;
                // mediaElement.Source = null;


                //danmu.ClearAll();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;

                GC.Collect();

            }
            catch (Exception ex)
            {
            }
        }


        private async void PostLocalHistory()
        {
            try
            {
                string url = string.Format("http://api.bilibili.com/x/history/add?_device=wp&_ulv=10000&access_key={0}&appkey={1}&build=5250000&platform=android", ApiHelper.access_key, ApiHelper._appKey);
                url += "&sign=" + ApiHelper.GetSign(url);
                string result = await WebClientClass.PostResults(new Uri(url), "aid=" + playNow.Aid);
            }
            catch (Exception)
            {
            }
        }


        private async void PostWatch()
        {
            try
            {
                if (!ApiHelper.IsLogin())
                {
                    return;
                }
                string url = "http://api.bilibili.com/x/v2/history/report";
                string content = string.Format("access_key={0}&aid={1}&appkey={2}&build=5250000&cid={3}&epid={6}&platform=android&progress=1&realtime=1&sid={4}&ts={5}&type=4", ApiHelper.access_key, playNow.Aid, ApiHelper._appKey, playNow.Mid, playNow.banId, ApiHelper.GetTimeSpan_2, playNow.episode_id);
                content += "&sign=" + ApiHelper.GetSign(content);

                //string url = string.Format("http://bangumi.bilibili.com/api/report_watch?access_key={0}&appkey={1}&build=5250000&cid={2}&mobi_app=win&platform=android&scale=xhdpi&ts={3}&episode_id={4}", ApiHelper.access_key, ApiHelper._appKey, playNow.Mid, ApiHelper.GetTimeSpan_2, playNow.episode_id);
                //url += "&sign=" + ApiHelper.GetSign(url);
                string result = await WebClientClass.PostResults(new Uri(url), content);
            }
            catch (Exception)
            {
            }
        }


        private async void HeartBeat(HeartBeatType heartBeatType)
        {
            try
            {
                if (!ApiHelper.IsLogin())
                {
                    return;
                }
                int time = 0;
                int play_type = 1;
                switch (heartBeatType)
                {
                    case HeartBeatType.Start:
                        play_type = 1;
                        time = 0;
                        break;
                    case HeartBeatType.Play:
                        play_type = 0;
                        time = Convert.ToInt32(mediaElement.Position.TotalSeconds);
                        break;
                    case HeartBeatType.End:
                        play_type = 4;
                        time = -1;
                        break;
                    default:
                        break;
                }


                string url = "http://api.bilibili.com/x/v2/history/report";

                string content = $"access_key={ApiHelper.access_key}&appkey={ApiHelper._appKey}&build={ApiHelper.build}&mobi_app=android&platform=android&";
                switch (playNow.Mode)
                {
                    case PlayMode.Bangumi:
                    case PlayMode.VipBangumi:

                        content += $"aid={playNow.Aid}&cid={playNow.Mid}&mid={ApiHelper.GetUserId()}&progress={time}&realtime={time}&ts={ApiHelper.GetTimeSpan}&type=4&sub_type=1&epid={playNow.episode_id}&sid={playNow.banId}";
                        break;
                    case PlayMode.Video:
                    case PlayMode.Movie:
                        content += $"aid={playNow.Aid}&cid={playNow.Mid}&mid={ApiHelper.GetUserId()}&progress={time}&realtime={time}&ts={ApiHelper.GetTimeSpan}&type=3";
                        break;
                    default:
                        break;
                }




                // content = string.Format("access_key={0}&aid={1}&appkey={2}&build=5250000&cid={3}&epid={4}&from=0&mid={5}&mobi_app=android&platform=android&played_time={8}&playtype=2&sid={6}&start_ts={7}&sub_type=1&ts={7}&type=4",
                //ApiHelper.access_key, playNow.Aid, ApiHelper._appKey, playNow.Mid, playNow.episode_id, ApiHelper.GetUserId(), playNow.banId, ApiHelper.GetTimeSpan, mediaElement.Position.TotalSeconds);


                content += "&sign=" + ApiHelper.GetSign(content);

                //string url = string.Format("http://bangumi.bilibili.com/api/report_watch?access_key={0}&appkey={1}&build=5250000&cid={2}&mobi_app=win&platform=android&scale=xhdpi&ts={3}&episode_id={4}", ApiHelper.access_key, ApiHelper._appKey, playNow.Mid, ApiHelper.GetTimeSpan_2, playNow.episode_id);
                //url += "&sign=" + ApiHelper.GetSign(url);
                string result = await WebClientClass.PostResults(new Uri(url), content);
            }
            catch (Exception)
            {
            }
        }




        public void UpdateSetting()
        {
            //if (!SettingHelper.IsPc())
            //{
            //    btn_ShowInfo.Visibility = Visibility.Collapsed;
            //    btn_HideInfo.Visibility = Visibility.Collapsed;
            //    // danmu.fontSize = 16;
            //}


            SYEngine.Core.ForceSoftwareDecode = SettingHelper.Get_ForceVideo();

            DanDis_Get();
            DMZZBDS = SettingHelper.Get_DMZZ();
            slider_DanmuSize.Value = SettingHelper.Get_NewDMSize();
            slider_Num.Value = SettingHelper.Get_DMNumber();
            slider_DanmuTran.Value = SettingHelper.Get_NewDMTran();
            slider_DanmuSpeed.Value = SettingHelper.Get_DMSpeed();
            cb_Style.SelectedIndex = SettingHelper.Get_DMStyle();

            sw_DanmuBorder.IsOn = SettingHelper.Get_DMBorder();
            sw_MergeDanmu.IsOn = SettingHelper.Get_MergeDanmu();
            mergeDanmu = sw_MergeDanmu.IsOn;

            sw_DanmuNotSubtitle.IsOn = SettingHelper.Get_DanmuNotSubtitle();
            //danmu.notHideSubtitle = sw_DanmuNotSubtitle.IsOn;

            sw_BoldDanmu.IsOn = SettingHelper.Get_BoldDanmu();

            List<string> fonts = SystemHelper.GetSystemFontFamilies();
            cb_Font.ItemsSource = fonts;
            if (SettingHelper.Get_DanmuFont() != "")
            {
                cb_Font.SelectedIndex = fonts.IndexOf(SettingHelper.Get_DanmuFont());
            }
            else
            {
                cb_Font.SelectedIndex = fonts.IndexOf(cb_Font.FontFamily.Source);
            }



            mediaElement.Volume = SettingHelper.Get_Volume();

            MTC.Brightness = SettingHelper.Get_Light();

            DanmuNum = SettingHelper.Get_DMNumber();
            rb_defu.IsChecked = true;
            btn_ViewPost.Visibility = Visibility.Collapsed;

            //danmu.borderStyle = (NSDanmaku.Model.DanmakuBorderStyle)SettingHelper.Get_DMStyle();
            menu_setting_buttom.IsChecked = !SettingHelper.Get_DMVisBottom();
            menu_setting_top.IsChecked = !SettingHelper.Get_DMVisTop();
            menu_setting_gd.IsChecked = !SettingHelper.Get_DMVisRoll();


        }



        string DMZZBDS = "";
        bool hidePointerFlag = false;
        int DanmuNum = 0;
        int i = 0;
        bool mergeDanmu = false;
        List<string> sended = new List<string>();
        private void Timer_Date_Tick(object sender, object e)
        {


            if (_PointerHideTime >= 5 && !hidePointerFlag)
            {
                Window.Current.CoreWindow.PointerCursor = null;
            }
            _PointerHideTime++;

            try
            {
                if (mediaElement.CurrentState == MediaElementState.Playing && LoadDanmu)
                {
                    if (DanMuPool != null)
                    {
                        int now_num = 0;

                        var pool = DanMuPool.Where(x => Convert.ToInt32(x.time) == Convert.ToInt32(mediaElement.Position.TotalSeconds));
                        foreach (var item in pool)
                        {
                            if (!DanDis_Dis(item.text))
                            {
                                if (now_num >= DanmuNum && DanmuNum != 0)
                                {
                                    return;
                                }
                                try
                                {
                                    if (DMZZBDS.Length != 0 && Regex.IsMatch(item.source, DMZZBDS))
                                    {
                                        return;
                                    }
                                }
                                catch (Exception)
                                {
                                }
                                if (mergeDanmu)
                                {
                                    if (sended.Contains(item.text + item.location))
                                    {
                                        return;
                                    }
                                    sended.Add(item.text + item.location);
                                }

                                switch (item.location)
                                {

                                    case NSDanmaku.Model.DanmakuLocation.Top:
                                        danmu.AddTopDanmu(item, false);
                                        break;
                                    case NSDanmaku.Model.DanmakuLocation.Bottom:
                                        danmu.AddBottomDanmu(item, false);
                                        break;
                                    case NSDanmaku.Model.DanmakuLocation.Position:
                                        danmu.AddPositionDanmu(item);
                                        break;
                                    default:
                                        danmu.AddRollDanmu(item, false);
                                        break;
                                }

                                now_num++;
                            }



                        }


                    }


                }
            }
            catch (Exception ex)
            {

            }

            sended.Clear();
        }



        int n = 0;
        private async void Timer_Tick(object sender, object e)
        {
            n++;
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (SqlHelper.GetPostIsViewPost(playNow.Mid))
                {
                    SqlHelper.UpdateViewPost(new ViewPostHelperClass() { epId = playNow.Mid, Post = Convert.ToInt32(mediaElement.Position.TotalSeconds) });
                }
                if (n == 10)
                {
                    if (mediaElement.CurrentState == MediaElementState.Playing)
                    {
                        HeartBeat(HeartBeatType.Play);
                    }
                    //if (mediaElement.NaturalDuration.TimeSpan.TotalSeconds>60*10)
                    //{
                    //    GC.Collect();
                    //}
                    n = 0;
                }
                //sql.UpdateValue(Cid, Convert.ToInt32(mediaElement.Position.TotalSeconds));
            });
        }

        #region 弹幕设置
        /// <summary>
        /// 弹幕屏蔽
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Dis_Remove_Click(object sender, RoutedEventArgs e)
        {
            foreach (NSDanmaku.Model.DanmakuModel item in list_DisDanmu.SelectedItems)
            {
                DanDis_Add(item.sendID, true);
                danmu.Remove(item);
                list_DisDanmu.Items.Remove(item);
            }
        }
        List<string> Guanjianzi = new List<string>();
        List<string> Yonghu = new List<string>();
        private void DanDis_Get()
        {


            string a = SettingHelper.Get_Guanjianzi();
            string b = SettingHelper.Get_Yonghu();
            if (a.Length != 0)
            {

                Guanjianzi = a.Split('|').ToList();
                Yonghu = b.Split('|').ToList();
                Guanjianzi.Remove(string.Empty);
                Yonghu.Remove(string.Empty);
            }


        }
        private bool DanDis_Dis(string text)
        {
            var a = (from sb in Guanjianzi where text.Contains(sb) select sb).ToList();
            var b = (from sb in Yonghu where text.Contains(sb) select sb).ToList();
            if (b.Count != 0 || a.Count != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void DanDis_Add(string text, bool IsYonghu)
        {
            if (IsYonghu)
            {
                SettingHelper.Set_Yonghu(SettingHelper.Get_Yonghu() + "|" + text);
                Yonghu.Add(text);
            }
            else
            {
                SettingHelper.Set_Guanjianzi(SettingHelper.Get_Guanjianzi() + "|" + text);

                Guanjianzi.Add(text);
            }

        }
        #endregion


        //private async void GetPlayUrl(string cid)
        //{
        //    string url = "http://interface.bilibili.com/playurl?_device=uwp&cid=" + cid + "&otype=xml&quality=" + 2 + "&appkey=" + ApiHelper._appKey + "&access_key=" + ApiHelper.access_key + "&type=mp4&mid=" + "" + "&_buvid=" + ApiHelper._buvid + "&_hwid=" + ApiHelper._hwid + "&platform=uwp_desktop" + "&ts=" + ApiHelper.GetTimeSpan;
        //    url += "&sign=" + ApiHelper.GetSign(url);
        //    string re = await WebClientClass.GetResults_Phone(new Uri(url));
        //    re = await WebClientClass.GetResults_Phone(new Uri(url));
        //    string playUrl = Regex.Match(re, "<url>(.*?)</url>").Groups[1].Value;
        //    playUrl = playUrl.Replace("<![CDATA[", "");
        //    playUrl = playUrl.Replace("]]>", "");
        //    mediaElement.Source = new Uri(playUrl);
        //}

        bool QuityLoading = false;
        private async void OpenVideo()
        {
            try
            {

                if (gv_play.Items.Count == 0 || gv_play.Items.Count == 1)
                {
                    MTC.ShowPlayListBtn = false;
                    MTC.ShowNextButton = false;
                    MTC.ShowPreviousButton = false;
                }
                else
                {
                    MTC.ShowPlayListBtn = true;
                    MTC.ShowPreviousButton = true;
                    MTC.ShowNextButton = true;
                    if (gv_play.SelectedIndex == 0)
                    {
                        MTC.ShowPreviousButton = false;
                    }
                    if (gv_play.SelectedIndex == gv_play.Items.Count - 1)
                    {
                        MTC.ShowNextButton = false;
                    }
                }

                LastPost = 0;
                MTC.ClearLog();
                //txt_Title.Text = playNow.Title + " - " + playNow.No + " " + playNow.VideoTitle;
                MTC.VideoTitle = playNow.Title + " - " + playNow.VideoTitle;
                MTC.ShowSendDanmuBtn = true;
                MTC.ShowDanmakuBtn = Visibility.Visible;
                //btn_Open_CloseDanmu.Visibility = Visibility.Visible;
                cb_Quity.Visibility = Visibility.Visible;

                pr.Text = "正在初始化播放器...";
                AddLog("正在初始化播放器...");
                await LoadQualities();
                txt_fvideo.Text = SettingHelper.Get_ForceVideo().ToString();
                AddLog("强制软解视频：" + txt_fvideo.Text);


                NSDanmaku.Helper.DanmakuParse danmakuParse = new NSDanmaku.Helper.DanmakuParse();
                if (!playLocal)
                {

                    cb_Quity.IsEnabled = true;
                    switch (playNow.Mode)
                    {
                        case PlayMode.Bangumi:
                        case PlayMode.Movie:
                        case PlayMode.VipBangumi:
                            pr.Text = "填充弹幕中...";
                            AddLog("开始填充弹幕...");

                            DanMuPool = await danmakuParse.ParseBiliBili(Convert.ToInt64(playNow.Mid));

                            pr.Text = "开始读取视频...";
                            AddLog(string.Format("开始读取视频{0}-{1}-{2}...", "anime", playNow.banId, playNow.Mid));
                            //string url = await ApiHelper.GetBiliUrl_Ban(playNow, cb_Quity.SelectedIndex + 1);
                            //playNow.Path = url;
                            //mediaElement.Source = new Uri(url);

                            var ban = await PlayurlHelper.GetBangumiUrl(playNow, (cb_Quity.SelectedItem as QualityModel).qn);
                            txt_site.Text = ban.from;
                            if (ban.usePlayMode == UsePlayMode.System)
                            {

                                mediaElement.Source = new Uri(ban.url);
                            }
                            else
                            {
                                mediaElement.Source = await ban.playlist.SaveAndGetFileUriAsync();
                            }
                            AddLog("播放器类型:" + ban.usePlayMode.ToString());
                            break;


                        case PlayMode.Video:

                            pr.Text = "填充弹幕中...";
                            AddLog("开始填充弹幕...");
                            DanMuPool = await danmakuParse.ParseBiliBili(Convert.ToInt64(playNow.Mid));
                            pr.Text = "加载视频中...";
                            AddLog(string.Format("开始读取视频{0}-{1}-{2}...", "video", playNow.Aid, playNow.Mid));
                            var ss = await PlayurlHelper.GetVideoUrl(playNow.Aid, playNow.Mid, (cb_Quity.SelectedItem as QualityModel).qn);
                            txt_site.Text = ss.from;
                            if (ss.usePlayMode == UsePlayMode.System)
                            {
                                mediaElement.Source = new Uri(ss.url);
                            }
                            else
                            {
                                mediaElement.Source = await ss.playlist.SaveAndGetFileUriAsync();
                            }

                            break;
                        case PlayMode.QQ:
                            AddLog("不支持播放的源:腾讯");
                            break;
                        case PlayMode.Sohu:

                            pr.Text = "填充弹幕中...";
                            AddLog("开始填充弹幕...");
                            DanMuPool = await danmakuParse.ParseBiliBili(Convert.ToInt64(playNow.Mid));
                            pr.Text = "加载视频中...";
                            AddLog(string.Format("开始读取视频{0}-{1}...", "sohu", playNow.Mid));
                            mediaElement.Source = new Uri(await PlayurlHelper.GetSoHuPlayInfo(playNow.rich_vid, cb_Quity.SelectedIndex + 1));
                            txt_site.Text = "sohu";
                            break;
                        case PlayMode.Local:

                            pr.Text = "加载视频中...";

                            MTC.ShowShareBtn = Visibility.Collapsed;
                            MTC.ShowCoinsBtn = Visibility.Collapsed;
                            cb_Quity.Visibility = Visibility.Collapsed;
                            await PlayLocal();
                            txt_site.Text = "本地";
                            break;
                        case PlayMode.FormLocal:
                            pr.Text = "加载视频中...";
                            AddLog("读取本地视频...");
                            MTC.ShowSendDanmuBtn = false;

                            MTC.ShowDanmakuBtn = Visibility.Collapsed;
                            cb_Quity.Visibility = Visibility.Collapsed;
                            txt_site.Text = "本地";
                            PlayFromLocal();
                            break;
                        default:
                            break;
                    }

                }
                else
                {
                    AddLog("读取本地视频...");
                    MTC.ShowDanmakuBtn = Visibility.Collapsed;
                    cb_Quity.Visibility = Visibility.Collapsed;
                    MTC.ShowSendDanmuBtn = false;
                    StorageFile file = await StorageFile.GetFileFromPathAsync(playNow.Mid);
                    IRandomAccessStream readStream = await file.OpenAsync(FileAccessMode.Read);
                    // var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    mediaElement.SetSource(readStream, file.ContentType);
                }
                AddLog("准备开始播放...");
                HeartBeat(HeartBeatType.Start);
                MTC.HideLog();
                MTC.timer2.Start();
                if (SqlHelper.GetPostIsViewPost(playNow.Mid) && SqlHelper.GettViewPost(playNow.Mid).Post != 0)
                {
                    TimeSpan ts = new TimeSpan(0, 0, SqlHelper.GettViewPost(playNow.Mid).Post);
                    LastPost = SqlHelper.GettViewPost(playNow.Mid).Post;
                    btn_ViewPost.Content = "上次播放到" + ts.Hours.ToString("00") + ":" + ts.Minutes.ToString("00") + ":" + ts.Seconds.ToString("00");
                    btn_ViewPost.Visibility = Visibility.Visible;
                    _lastpost_in.Begin();
                    await Task.Delay(5000);
                    _lastpost_out.Begin();
                }
                else
                {
                    if (!SqlHelper.GetPostIsViewPost(playNow.Mid))
                    {
                        SqlHelper.AddViewPost(new ViewPostHelperClass() { epId = playNow.Mid, Post = 0, viewTime = DateTime.Now.ToLocalTime() });
                    }
                }


            }
            catch (Exception ex)
            {
                AddLog("视频播放失败了" + ex.HResult);
                await new MessageDialog("无法读取到播放地址 ＞﹏＜ \r\n请尝试登录、更换清晰度或FQ后再试\r\n如果是地区限制番可能无法第一时间观看，请过段时间重试").ShowAsync();
            }
            finally
            {

                PostLocalHistory();
                if (playNow.episode_id != null && playNow.episode_id.Length != 0)
                {
                    PostWatch();
                }
            }
        }

        private async void ChangeQuality()
        {
            switch (playNow.Mode)
            {
                case PlayMode.Bangumi:
                case PlayMode.Movie:
                case PlayMode.VipBangumi:

                    var ban = await PlayurlHelper.GetBangumiUrl(playNow, (cb_Quity.SelectedItem as QualityModel).qn);

                    if (ban.usePlayMode == UsePlayMode.System)
                    {

                        mediaElement.Source = new Uri(ban.url);
                    }
                    else
                    {
                        mediaElement.Source = await ban.playlist.SaveAndGetFileUriAsync();
                    }

                    break;


                case PlayMode.Video:


                    var ss = await PlayurlHelper.GetVideoUrl(playNow.Aid, playNow.Mid, (cb_Quity.SelectedItem as QualityModel).qn);

                    if (ss.usePlayMode == UsePlayMode.System)
                    {
                        mediaElement.Source = new Uri(ss.url);
                    }
                    else
                    {
                        mediaElement.Source = await ss.playlist.SaveAndGetFileUriAsync();
                    }

                    break;
                case PlayMode.Sohu:
                    mediaElement.Source = new Uri(await PlayurlHelper.GetSoHuPlayInfo(playNow.rich_vid, cb_Quity.SelectedIndex + 1));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 读取清晰度
        /// </summary>
        private async Task LoadQualities()
        {
            AddLog("正在获取视频清晰度");
            QuityLoading = true;
            switch (playNow.Mode)
            {
                case PlayMode.Bangumi:
                case PlayMode.Movie:
                case PlayMode.VipBangumi:
                    cb_Quity.ItemsSource = await PlayurlHelper.GetAnimeQualities(playNow);
                    break;
                case PlayMode.Video:
                    cb_Quity.ItemsSource = await PlayurlHelper.GetVideoQualities(playNow);
                    break;
                case PlayMode.QQ:
                    cb_Quity.ItemsSource = new List<QualityModel>() { new QualityModel() { description = "默认", qn = 64 } };
                    AddLog("不支持的播放源:腾讯");
                    break;
                case PlayMode.Sohu:
                    cb_Quity.ItemsSource = PlayurlHelper.GetDefaultQualities();
                    break;
                default:
                    cb_Quity.ItemsSource = new List<QualityModel>() { new QualityModel() { description = "默认", qn = 64 } };
                    break;
            }

            var settingq = SettingHelper.Get_NewQuality();
            var ls = (cb_Quity.ItemsSource as List<QualityModel>).Where(x => x.qn == settingq).ToList();
            if (ls != null && ls.Count != 0)
            {
                cb_Quity.SelectedItem = ls[0];
            }
            else
            {
                cb_Quity.SelectedIndex = cb_Quity.Items.Count - 1;
            }
            QuityLoading = false;

        }

        private void AddLog(string msg)
        {
            MTC.AddLog(msg);
            //txt_log.Text += string.Format("[{0}]{1}\r\n",DateTime.Now.ToString("HH:mm:ss"),msg);
        }


        private async void PlayFromLocal()
        {
            var item = playNow.Parameter as StorageFile;
            //if (item .FileType== ".mp4")
            //{
            IRandomAccessStream readStream = await item.OpenAsync(FileAccessMode.Read);
            // var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            mediaElement.SetSource(readStream, item.ContentType);
            //if (SettingHelper.Get_FFmpeg())
            //{

            //    mediaElement_MediaFailed(null, null);
            //}
            //}

        }
        private async Task PlayLocal()
        {
            AddLog("开始读取本地视频...");
            NSDanmaku.Helper.DanmakuParse danmakuParse = new NSDanmaku.Helper.DanmakuParse();
            StorageFolder f = await StorageFolder.GetFolderFromPathAsync(playNow.Path);
            var ls = await f.GetFilesAsync();
            var paths = new List<string>();
            foreach (var item in ls)
            {
                if (item.FileType == ".xml")
                {
                    pr.Text = "填充弹幕中...";
                    AddLog("填充弹幕中...");
                    DanMuPool = await danmakuParse.ParseBiliBili(item);
                }
                if (item.FileType == ".mp4" || item.FileType == ".flv")
                {
                    paths.Add(item.Path);
                    playNow.Parameter = item;
                    // IRandomAccessStream readStream = await item.OpenAsync(FileAccessMode.Read);
                    // var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    // mediaElement.SetSource(readStream, item.ContentType);

                }

            }
            var s = await PlayLocalVideo(paths);
            mediaElement.SetMediaStreamSource(s);
            //playNow.Path
        }

        private async Task<MediaStreamSource> PlayLocalVideo(List<string> paths)
        {
            MediaComposition composition = new MediaComposition();
            foreach (var item in paths)
            {

                var file = await StorageFile.GetFileFromPathAsync(item);
                var clip = await MediaClip.CreateFromFileAsync(file);
                composition.Clips.Add(clip);

            }
            return composition.GenerateMediaStreamSource();

        }


        private async Task<List<MyDanmaku.DanMuModel>> GetLocalDanmu(StorageFile danmuFile)
        {
            List<MyDanmaku.DanMuModel> ls = new List<MyDanmaku.DanMuModel>();
            try
            {
                string a = await FileIO.ReadTextAsync(danmuFile);
                XmlDocument xdoc = new XmlDocument();
                a = Regex.Replace(a, @"[\x00-\x08]|[\x0B-\x0C]|[\x0E-\x1F]", "");
                xdoc.LoadXml(a);
                XmlElement el = xdoc.DocumentElement;
                XmlNodeList xml = el.ChildNodes;
                foreach (XmlNode item in xml)
                {
                    if (item.Attributes["p"] != null)
                    {
                        try
                        {
                            string heheda = item.Attributes["p"].Value;
                            string[] haha = heheda.Split(',');
                            ls.Add(new MyDanmaku.DanMuModel
                            {
                                DanTime = decimal.Parse(haha[0]),
                                DanMode = haha[1],
                                DanSize = haha[2],
                                _DanColor = haha[3],
                                DanSendTime = haha[4],
                                DanPool = haha[5],
                                DanID = haha[6],
                                DanRowID = haha[7],
                                DanText = item.InnerText,
                                source = item.OuterXml
                            });
                        }
                        catch (Exception)
                        {
                        }

                    }
                }
                AddLog("填充弹幕成功，共" + ls.Count + "条");
                return ls;
            }
            catch (Exception)
            {
                AddLog("弹幕加载失败了...");
                return ls;
            }
        }

        private void btn_Back_Click(object sender, RoutedEventArgs e)
        {
            //if (this.Frame.CanGoBack)
            //{
            //    mediaElement.Stop();
            //    this.Frame.GoBack();
            //}
        }

        public async Task<List<MyDanmaku.DanMuModel>> GetDM(string cid, bool IsLocal, bool IsOld, string path)
        {
            List<MyDanmaku.DanMuModel> ls = new List<MyDanmaku.DanMuModel>();
            try
            {

                string a = await WebClientClass.GetResults(new Uri("http://comment.bilibili.com/" + cid + ".xml" + "?rnd=" + new Random().Next(1, 9999)));
                XmlDocument xdoc = new XmlDocument();
                a = Regex.Replace(a, @"[\x00-\x08]|[\x0B-\x0C]|[\x0E-\x1F]", "");
                xdoc.LoadXml(a);
                XmlElement el = xdoc.DocumentElement;
                XmlNodeList xml = el.ChildNodes;
                foreach (XmlNode item in xml)
                {
                    if (item.Attributes["p"] != null)
                    {
                        try
                        {
                            string heheda = item.Attributes["p"].Value;
                            string[] haha = heheda.Split(',');
                            ls.Add(new MyDanmaku.DanMuModel
                            {
                                DanTime = decimal.Parse(haha[0]),
                                DanMode = haha[1],
                                DanSize = haha[2],
                                _DanColor = haha[3],
                                DanSendTime = haha[4],
                                DanPool = haha[5],
                                DanID = haha[6],
                                DanRowID = haha[7],
                                DanText = item.InnerText,
                                source = item.OuterXml
                            });
                        }
                        catch (Exception)
                        {
                        }

                    }
                }
                //if (ls.Count>10000)
                //{
                //    ls = ls.Take(6000).ToList();
                //}  
                AddLog("填充弹幕成功，共" + ls.Count + "条");
                return ls;
            }
            catch (Exception)
            {
                AddLog("弹幕加载失败了...");
                return ls;
            }

        }


        private void btn_Play_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Play();

        }

        private void btn_Pause_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.CanPause)
            {
                mediaElement.Pause();
            }
        }

        private async void mediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            //if (e.ErrorMessage.Contains("SRC_NOT_SUPPORT"))
            //{
            //    await new MessageDialog("暂时无法播放此视频，请稍后再试").ShowAsync();
            //}
            //else
            //{
            //    await new MessageDialog("无法播放此视频" + e.ErrorMessage).ShowAsync();

            //}
            await new MessageDialog("无法播放此视频 ＞﹏＜ \r\n请尝试更换清晰度").ShowAsync();
        }
        private void mediaElement_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void mediaElement_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {


        }


        private void mediaElement_BufferingProgressChanged(object sender, RoutedEventArgs e)
        {

            pr.Text = mediaElement.BufferingProgress.ToString("P");
        }
        bool buffering = false;
        private void mediaElement_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            buffering = false;
            switch (mediaElement.CurrentState)
            {
                case MediaElementState.Closed:
                    if (_systemMediaTransportControls != null)
                    {
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
                    }


                    break;
                case MediaElementState.Opening:

                    progress.Visibility = Visibility.Visible;

                    break;
                case MediaElementState.Buffering:
                    buffering = true;
                    progress.Visibility = Visibility.Visible;
                    danmu.PauseDanmaku();
                    break;
                case MediaElementState.Playing:
                    if (_systemMediaTransportControls != null)
                    {
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                    }

                    progress.Visibility = Visibility.Collapsed;
                    danmu.ResumeDanmaku();

                    if (timer != null)
                    {
                        timer.Start();
                    }
                    mediaElement.PlaybackRate = slider_Rate.Value;

                    break;
                case MediaElementState.Paused:
                    if (_systemMediaTransportControls != null)
                    {
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                    }

                    progress.Visibility = Visibility.Collapsed;
                    danmu.PauseDanmaku();
                    if (timer != null)
                    {
                        timer.Stop();
                    }
                    break;
                case MediaElementState.Stopped:
                    if (_systemMediaTransportControls != null)
                    {
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                    }

                    progress.Visibility = Visibility.Collapsed;
                    danmu.ClearAll();
                    if (timer != null)
                    {
                        timer.Stop();
                    }

                    break;
                default:
                    break;
            }
        }


        #region 手势操作
        private void ss_Volume_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;

            txt_SSPosition.Visibility = Visibility.Visible;
            double Y = e.Delta.Translation.Y;
            if (Y > 0)
            {
                double dd = Y / (ss_Volume.ActualHeight * 0.8);

                //slider_V.Value -= d;
                mediaElement.Volume -= dd;
            }
            else
            {
                double dd = Math.Abs(Y) / (ss_Volume.ActualHeight * 0.8);
                mediaElement.Volume += dd;
                //slider_V.Value += d;
            }
            txt_SSPosition.Text = "音量:" + mediaElement.Volume.ToString("P");
            //Utils.ShowMessageToast("音量:" + mediaElement.Volume.ToString("P"), 3000);
        }

        private void ss_Volume_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            e.Handled = true;
            txt_SSPosition.Text = "";
            txt_SSPosition.Visibility = Visibility.Collapsed;
        }

        private void ss_Light_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;

            txt_SSPosition.Visibility = Visibility.Visible;
            double Y = e.Delta.Translation.Y;
            if (Y > 0)
            {
                double dd = Y / (ss_Light.ActualHeight * 0.8);
                MTC.Brightness += dd;
            }
            else
            {
                double dd = Math.Abs(Y) / (ss_Light.ActualHeight * 0.8);
                MTC.Brightness -= dd;

            }
            if (MTC.Brightness < 0)
            {
                MTC.Brightness = 0;
            }
            if (MTC.Brightness > 1)
            {
                MTC.Brightness = 1;
            }
            txt_SSPosition.Text = "亮度:" + Math.Abs(MTC.Brightness-1).ToString("P");

        }

        private void ss_Light_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            e.Handled = true;
            txt_SSPosition.Text = "";
            txt_SSPosition.Visibility = Visibility.Collapsed;
        }


        double ssValue = 0;
        private void Grid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;
            if (mediaElement.CurrentState != MediaElementState.Playing)
            {
                return;
            }
            //progress.Visibility = Visibility.Visible;
            double X = e.Delta.Translation.X;
            if (X > 0)
            {
                double dd = X / this.ActualWidth;
                double d = dd * 90;
                ssValue += d;
                //slider.Value += d;
            }
            else
            {
                double dd = Math.Abs(X) / this.ActualWidth;
                double d = dd * 90;
                ssValue -= d;
                //slider.Value -= d;
            }
            TimeSpan ts = mediaElement.Position;
            ts = ts.Add(TimeSpan.FromSeconds(ssValue));
            //txt_Post.Text = ts.Hours.ToString("00") + ":" + ts.Minutes.ToString("00") + ":" + ts.Seconds.ToString("00") + "/" + mediaElement.NaturalDuration.TimeSpan.Hours.ToString("00") + ":" + mediaElement.NaturalDuration.TimeSpan.Minutes.ToString("00") + ":" + mediaElement.NaturalDuration.TimeSpan.Seconds.ToString("00");

            txt_SSPosition.Text = ts.Hours.ToString("00") + ":" + ts.Minutes.ToString("00") + ":" + ts.Seconds.ToString("00");
            //Utils.ShowMessageToast(ts.Hours.ToString("00") + ":" + ts.Minutes.ToString("00") + ":" + ts.Seconds.ToString("00"), 3000);
        }

        private void Grid_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            e.Handled = true;

            double X = e.Cumulative.Translation.X;
            if (ssValue != 0)
            {
                mediaElement.Position = mediaElement.Position.Add(TimeSpan.FromSeconds(ssValue));
            }
            txt_SSPosition.Visibility = Visibility.Collapsed;
        }

        private void MTC_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            e.Handled = true;
            ssValue = 0;
            txt_SSPosition.Text = "";
            txt_SSPosition.Visibility = Visibility.Visible;
        }


        #endregion

        private void gv_play_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gv_play.SelectedIndex != -1)
            {
                playNow = gv_play.SelectedItem as PlayerModel;

                cb_Quity.ItemsSource = null;

                // PlayerEvent(gv_play.SelectedIndex);
                OpenVideo();

            }
        }

        private void btn_Select_Click(object sender, RoutedEventArgs e)
        {

            gv_play.Visibility = Visibility.Visible;
            grid_Setting.Visibility = Visibility.Collapsed;
            grid_DM.Visibility = Visibility.Collapsed;
            grid_Info.Visibility = Visibility.Collapsed;
            grid_PB.Visibility = Visibility.Collapsed;

            sp_View.IsPaneOpen = true;
        }

        private async void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            try
            {
                HeartBeat(HeartBeatType.End);
                if (cb_setting_1.IsChecked.Value)
                {
                    mediaElement.Play();
                    danmu.ClearAll();
                    return;
                }
                if (gv_play.SelectedIndex == gv_play.Items.Count - 1)
                {
                    if (cb_setting_2.IsChecked.Value)
                    {
                        gv_play.SelectedIndex = 0;
                    }
                    else
                    {
                        Utils.ShowMessageToast("全部看完了", 3000);
                    }
                }
                else
                {
                    mediaElement.Stop();
                    Utils.ShowMessageToast("3秒后播放下一集", 3000);
                    await Task.Delay(3000);
                    gv_play.SelectedIndex += 1;
                }
            }
            catch (Exception)
            {
            }

        }

        private void cb_Quity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (!QuityLoading && cb_Quity.SelectedItem != null)
            {
                SettingHelper.Set_NewQuality((cb_Quity.SelectedItem as QualityModel).qn);
                mediaElement.Stop();
                ChangeQuality();
            }
            //if (gv_play.ItemsSource == null)
            //{
            //    return;
            //}
            //mediaElement.Stop();
            //SettingHelper.Set_PlayQualit(cb_Quity.SelectedIndex + 1);
            //OpenVideo();
        }
        private void _lastpost_out_Completed(object sender, object e)
        {
            btn_ViewPost.Visibility = Visibility.Collapsed;
        }

        private void btn_ViewPost_Click(object sender, RoutedEventArgs e)
        {
            if (LastPost != 0)
            {
                mediaElement.Position = new TimeSpan(0, 0, Convert.ToInt32(LastPost));
                btn_ViewPost.Visibility = Visibility.Collapsed;

            }
        }

        private void ss_Holding(object sender, HoldingRoutedEventArgs e)
        {
            menu.ShowAt(this);
        }

        private void btn_VideoInfo_Click(object sender, RoutedEventArgs e)
        {

            sp_View.IsPaneOpen = true;
            grid_Setting.Visibility = Visibility.Visible;
            gv_play.Visibility = Visibility.Collapsed;
            grid_DM.Visibility = Visibility.Collapsed;
            grid_Info.Visibility = Visibility.Collapsed;
            grid_PB.Visibility = Visibility.Collapsed;
            //string info = string.Format("视频高度：{0}\r\n视频宽度：{1}\r\n视频长度：{2}\r\n缓冲进度:{3}", mediaElement.NaturalVideoHeight, mediaElement.NaturalVideoWidth, mediaElement.NaturalDuration.TimeSpan.Hours.ToString("00") + ":" + mediaElement.NaturalDuration.TimeSpan.Minutes.ToString("00") + ":" + mediaElement.NaturalDuration.TimeSpan.Seconds.ToString("00"), mediaElement.DownloadProgress.ToString("P"));
            //await new MessageDialog(info, "视频信息").ShowAsync();
        }
        private void cb_setting_defu_Checked(object sender, RoutedEventArgs e)
        {
            if (mediaElement == null)
            {
                return;
            }
            mediaElement.Width = this.ActualWidth;
            mediaElement.Height = this.ActualHeight;
            mediaElement.Stretch = Stretch.Uniform;
        }

        private void cb_setting_43_Checked(object sender, RoutedEventArgs e)
        {
            if (mediaElement == null)
            {
                return;
            }
            mediaElement.Stretch = Stretch.Fill;
            mediaElement.Height = this.ActualHeight;
            mediaElement.Width = this.ActualHeight * 4 / 3;
        }

        private void cb_setting_169_Checked(object sender, RoutedEventArgs e)
        {
            if (mediaElement == null)
            {
                return;
            }
            mediaElement.Stretch = Stretch.Fill;
            mediaElement.Height = this.ActualHeight;
            mediaElement.Width = this.ActualHeight * 16 / 9;


        }

        //private void btn_HideInfo_Click(object sender, RoutedEventArgs e)
        //{
        //    //ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
        //    //MaxWIndowsEvent(true);
        //    btn_HideInfo.Visibility = Visibility.Collapsed;
        //    btn_ShowInfo.Visibility = Visibility.Visible;
        //    danmu.SetJJ();
        //}

        //private void btn_ShowInfo_Click(object sender, RoutedEventArgs e)
        //{
        //    //MaxWIndowsEvent(false);
        //    //ApplicationView.GetForCurrentView().ExitFullScreenMode();
        //    btn_HideInfo.Visibility = Visibility.Visible;
        //    btn_ShowInfo.Visibility = Visibility.Collapsed;
        //    danmu.SetJJ();
        //}




        private void mediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_systemMediaTransportControls == null)
                {
                    return;
                }
                SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
                updater.Type = MediaPlaybackType.Video;
                // updater.MusicProperties.AlbumArtist = info.owner.name;
                updater.VideoProperties.Subtitle = playNow.VideoTitle;
                updater.VideoProperties.Title = playNow.Title;

                // Set the album art thumbnail.
                // RandomAccessStreamReference is defined in Windows.Storage.Streams
                updater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/Logo.png"));

                // Update the system media transport controls.
                updater.Update();
                var timelineProperties = new SystemMediaTransportControlsTimelineProperties();

                // Fill in the data, using the media elements properties 
                timelineProperties.StartTime = TimeSpan.FromSeconds(0);
                timelineProperties.MinSeekTime = TimeSpan.FromSeconds(0);
                timelineProperties.Position = mediaElement.Position;
                timelineProperties.MaxSeekTime = mediaElement.NaturalDuration.TimeSpan;
                timelineProperties.EndTime = mediaElement.NaturalDuration.TimeSpan;

                // Update the System Media transport Controls 
                _systemMediaTransportControls.UpdateTimelineProperties(timelineProperties);


            }
            catch (Exception)
            {

            }



        }



        private void menuitem_DM_Click(object sender, RoutedEventArgs e)
        {

            sp_View.IsPaneOpen = true;
            grid_Setting.Visibility = Visibility.Collapsed;
            gv_play.Visibility = Visibility.Collapsed;
            grid_DM.Visibility = Visibility.Visible;
            grid_Info.Visibility = Visibility.Collapsed;
            grid_PB.Visibility = Visibility.Collapsed;

        }

        private void menuitem_PB_Click(object sender, RoutedEventArgs e)
        {

            mediaElement.Pause();
            sp_View.IsPaneOpen = true;
            grid_Setting.Visibility = Visibility.Collapsed;
            gv_play.Visibility = Visibility.Collapsed;
            grid_DM.Visibility = Visibility.Collapsed;
            grid_Info.Visibility = Visibility.Collapsed;
            grid_PB.Visibility = Visibility.Visible;
            list_DisDanmu.Items.Clear();
            foreach (var item in danmu.GetDanmakus())
            {
                list_DisDanmu.Items.Add(item);
            }
        }

        private void menuitem_Info_Click(object sender, RoutedEventArgs e)
        {

            sp_View.IsPaneOpen = true;
            if (playNow != null && DanMuPool != null)
            {
                txt_Num.Text = DanMuPool.Count.ToString();
                txt_sId.Text = playNow.Aid;
                txt_eId.Text = playNow.Mid;
            }
            grid_Setting.Visibility = Visibility.Collapsed;
            gv_play.Visibility = Visibility.Collapsed;
            grid_DM.Visibility = Visibility.Collapsed;
            grid_Info.Visibility = Visibility.Visible;
            grid_PB.Visibility = Visibility.Collapsed;
        }

        #region 设置
        private void sw_DanmuBorder_Toggled(object sender, RoutedEventArgs e)
        {
            //danmu.D_Border = sw_DanmuBorder.IsOn;
            SettingHelper.Set_DMBorder(sw_DanmuBorder.IsOn);
        }

        private void slider_DanmuSize_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (danmu == null)
            {
                return;
            }
            danmu.sizeZoom = slider_DanmuSize.Value;

            SettingHelper.Set_NewDMSize(slider_DanmuSize.Value);
        }

        private void cb_Font_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //switch (cb_Font.SelectedIndex)
            //{
            //    case 0:
            //        danmu.fontFamily = "默认";
            //        break;
            //    case 1:
            //        danmu.fontFamily = "微软雅黑";
            //        break;
            //    case 2:
            //        danmu.fontFamily = "黑体";
            //        break;
            //    case 3:
            //        danmu.fontFamily = "楷体";
            //        break;
            //    case 4:
            //        danmu.fontFamily = "宋体";
            //        break;
            //    case 5:
            //        danmu.fontFamily = "等线";
            //        break;
            //    default:
            //        break;
            //}
            if (cb_Font.SelectedItem == null)
            {
                return;
            }
            SettingHelper.Set_DanmuFont(cb_Font.SelectedItem.ToString());
            danmu.font = cb_Font.SelectedItem.ToString();
        }

        private void slider_DanmuSpeed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (danmu == null)
            {
                return;
            }
            danmu.speed = Convert.ToInt32(slider_DanmuSpeed.Value);
            if (slider_DanmuSpeed.Value == 0 || slider_DanmuSpeed.Value == -1)
            {
                return;
            }
            SettingHelper.Set_DMSpeed(slider_DanmuSpeed.Value);
        }

        private void slider_DanmuTran_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (danmu == null)
            {
                return;
            }
            if (slider_DanmuTran.Value == 0 || slider_DanmuTran.Value == -1)
            {
                return;
            }
            danmu.Opacity = slider_DanmuTran.Value;
            SettingHelper.Set_NewDMTran(slider_DanmuTran.Value);
        }
        private void slider_Num_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (danmu == null)
            {
                return;
            }
            DanmuNum = Convert.ToInt32(slider_Num.Value);
            SettingHelper.Set_DMNumber(Convert.ToInt32(slider_Num.Value));
        }




        private void menu_setting_top_Click(object sender, RoutedEventArgs e)
        {

            danmu.HideDanmaku(NSDanmaku.Model.DanmakuLocation.Top);
            SettingHelper.Set_DMVisTop(false);


        }

        private void menu_setting_buttom_Click(object sender, RoutedEventArgs e)
        {
            danmu.HideDanmaku(NSDanmaku.Model.DanmakuLocation.Bottom);
            SettingHelper.Set_DMVisBottom(false);
        }

        private void menu_setting_gd_Checked(object sender, RoutedEventArgs e)
        {
            danmu.HideDanmaku(NSDanmaku.Model.DanmakuLocation.Roll);
            SettingHelper.Set_DMVisRoll(false);
        }

        private void menu_setting_gd_Unchecked(object sender, RoutedEventArgs e)
        {
            danmu.ShowDanmaku(NSDanmaku.Model.DanmakuLocation.Roll);
            SettingHelper.Set_DMVisRoll(true);
        }

        private void menu_setting_top_Unchecked(object sender, RoutedEventArgs e)
        {
            danmu.ShowDanmaku(NSDanmaku.Model.DanmakuLocation.Top);
            SettingHelper.Set_DMVisTop(true);
        }

        private void menu_setting_buttom_Unchecked(object sender, RoutedEventArgs e)
        {
            // danmu.SetDanmuVisibility(true, MyDanmaku.DanmuMode.Buttom);
            danmu.ShowDanmaku(NSDanmaku.Model.DanmakuLocation.Bottom);
            SettingHelper.Set_DMVisBottom(true);
        }


        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {

            DanDis_Add(txt_Dis.Text, false);
            txt_Dis.Text = "";
            var s = danmu.GetDanmakus();
            foreach (var item in s)
            {
                if (DanDis_Dis(item.text))
                {
                    danmu.Remove(item);
                }
            }
        }






        #endregion


        /// <summary>
        /// 发送弹幕
        /// </summary>
        //public async void SenDanmuKa()
        //{
        //    if (Send_text_Comment.Text.Length == 0)
        //    {
        //        Utils.ShowMessageToast("弹幕内容不能为空!", 2000);
        //        return;
        //    }
        //    if (!ApiHelper.IsLogin())
        //    {
        //        Utils.ShowMessageToast("请先登录!", 2000);
        //        return;
        //    }
        //    try
        //    {
        //        var url = string.Format("https://api.bilibili.com/comment/post?access_key={0}&aid={1}&appkey={2}&build=5250000&cid={3}&mobi_app=android&pid={4}&platform=android&ts={5}", ApiHelper.access_key, playNow.Aid, ApiHelper._appKey, playNow.Mid, ApiHelper.GetUserId(), ApiHelper.GetTimeSpan);
        //        url += "&sign=" + ApiHelper.GetSign(url);

        //        Uri ReUri = new Uri(url);


        //        int modeInt = 1;
        //        if (Send_cb_Mode.SelectedIndex == 2)
        //        {
        //            modeInt = 4;
        //        }
        //        if (Send_cb_Mode.SelectedIndex == 1)
        //        {
        //            modeInt = 5;
        //        }

        //        string data = string.Format("playTime={0}&pool=0&color={1}&screen_state=1&rnd={2}&from=0&type=json&msg={3}&cid={4}&fontsize=25&mode={5}&mid={6}",
        //            mediaElement.Position.TotalSeconds.ToString(),
        //            ((ComboBoxItem)Send_cb_Color.SelectedItem).Tag,
        //            new Random().Next(1, 99999999),
        //            Uri.EscapeDataString(Send_text_Comment.Text),
        //            playNow.Mid, modeInt, ApiHelper.GetUserId()
        //            );

        //        //string Canshu = "message=" + Send_text_Comment.Text + "&pool=0&playTime=" + mediaElement.Position.TotalSeconds.ToString() + "&cid=" + playNow.Mid + "&date=" + DateTime.Now.ToString() + "&fontsize=25&mode=" + modeInt + "&rnd=" + new Random().Next(100000000, 999999999) + "&color=" + ((ComboBoxItem)Send_cb_Color.SelectedItem).Tag;
        //        string result = await WebClientClass.PostResults(ReUri, data);
        //        var obj = JObject.Parse(result);

        //        if (Convert.ToInt32(obj["code"].ToString()) != 0)
        //        {
        //            Utils.ShowMessageToast("弹幕发送失败" + obj["message"].ToString(), 3000);
        //        }
        //        else
        //        {
        //            Utils.ShowMessageToast("弹幕成功发射", 3000);
        //            if (modeInt == 1)
        //            {
        //                //danmu.AddRollDanmu(new NSDanmaku.Model.DanmakuModel { text = Send_text_Comment.Text, color = ((ComboBoxItem)Send_cb_Color.SelectedItem).Tag.ToString(), DanSize = "25" }, true);
        //            }
        //            if (modeInt == 4)
        //            {
        //                //danmu.AddTopButtomDanmu(new MyDanmaku.DanMuModel { DanText = Send_text_Comment.Text, _DanColor = ((ComboBoxItem)Send_cb_Color.SelectedItem).Tag.ToString(), DanSize = "25" }, false, true);
        //            }
        //            if (modeInt == 5)
        //            {
        //                //danmu.AddTopButtomDanmu(new MyDanmaku.DanMuModel { DanText = Send_text_Comment.Text, _DanColor = ((ComboBoxItem)Send_cb_Color.SelectedItem).Tag.ToString(), DanSize = "25" }, true, true);
        //            }
        //            Send_text_Comment.Text = string.Empty;
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Utils.ShowMessageToast("发送弹幕发生错误！\r\n" + ex.HResult, 3000);
        //    }
        //}



        protected override Size MeasureOverride(Size availableSize)
        {

            return base.MeasureOverride(availableSize);
        }
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void mediaElement_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
            _PointerHideTime = 1;
        }
        int _PointerHideTime = 1;
        private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
            _PointerHideTime = 1;
        }



        private void btn_Dis_Report_Click(object sender, RoutedEventArgs e)
        {
            if (list_DisDanmu.SelectedItems.Count == 0)
            {
                return;
            }
            foreach (MyDanmaku.DanMuModel item in list_DisDanmu.SelectedItems)
            {
                ReportDM(item.DanRowID);
            }
        }

        private async void ReportDM(string dmid)
        {
            try
            {
                string results = await WebClientClass.PostResults(new Uri("http://interface.bilibili.com/dmreport"), string.Format("reportToAdmin=0&reason=&dm_inid={0}&dmid={1}", playNow.Mid, dmid), "http://www.bilibili.com");
                if (results == "0")
                {
                    Utils.ShowMessageToast("举报成功", 3000);
                }
                else
                {
                    Utils.ShowMessageToast("举报失败", 3000);
                }
            }
            catch (Exception)
            {
                Utils.ShowMessageToast("举报错误", 3000);
            }
        }

        private async void menuitem_UpdateDanmu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DanmakuParse danmakuParse = new DanmakuParse();
                DanMuPool = await danmakuParse.ParseBiliBili(Convert.ToInt64(playNow.Mid));
                Utils.ShowMessageToast("已经更新弹幕池", 3000);
            }
            catch (Exception)
            {
            }

        }


        private void sw_MergeDanmu_Toggled(object sender, RoutedEventArgs e)
        {
            SettingHelper.Set_MergeDanmu(sw_MergeDanmu.IsOn);
            mergeDanmu = sw_MergeDanmu.IsOn;
        }

        private void MTC_OpenDanmaku(object sender, bool e)
        {
            LoadDanmu = e;
        }

        private void MTC_ExitPlayer(object sender, EventArgs e)
        {
            this.Frame.GoBack();
        }

        private async void MTC_OnMiniWindows(object sender, EventArgs e)
        {
            if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
            {
                await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
                danmu.ClearAll();
                danmu.SetSpeed(5);
                danmu.sizeZoom = 0.5;
            }
        }

        private async void MTC_OnExitMiniWindows(object sender, EventArgs e)
        {
            await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
            danmu.ClearAll();
            danmu.speed = SettingHelper.Get_DMSpeed().ToInt32();
            danmu.sizeZoom = SettingHelper.Get_NewDMSize();
        }

        private void MTC_DanmakuSetting(object sender, EventArgs e)
        {
            sp_View.IsPaneOpen = true;
            grid_Setting.Visibility = Visibility.Collapsed;
            gv_play.Visibility = Visibility.Collapsed;
            grid_DM.Visibility = Visibility.Visible;
            grid_Info.Visibility = Visibility.Collapsed;
            grid_PB.Visibility = Visibility.Collapsed;
        }

        private void MTC_SelectList(object sender, EventArgs e)
        {
            gv_play.Visibility = Visibility.Visible;
            grid_Setting.Visibility = Visibility.Collapsed;
            grid_DM.Visibility = Visibility.Collapsed;
            grid_Info.Visibility = Visibility.Collapsed;
            grid_PB.Visibility = Visibility.Collapsed;

            sp_View.IsPaneOpen = true;
        }

        private void MTC_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (!SettingHelper.IsPc())
            {
                if (mediaElement.CurrentState == MediaElementState.Playing)
                {
                    mediaElement.Pause();
                }
                else
                {
                    mediaElement.Play();
                }
            }
            else
            {
                if (MTC.IsFullWindow)
                {
                    MTC.ExitFull();
                    //mediaElement.IsFullWindow = false;
                }
                else
                {
                    MTC.ToFull();
                    //mediaElement.IsFullWindow = true;
                }
            }
        }

        private void MTC_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (SettingHelper.IsPc() && e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                if (mediaElement.CurrentState == MediaElementState.Playing)
                {
                    mediaElement.Pause();
                }
                else
                {
                    mediaElement.Play();
                }
            }
        }

        private void MTC_Next(object sender, EventArgs e)
        {
            gv_play.SelectedIndex += 1;
        }

        private void MTC_Previous(object sender, EventArgs e)
        {
            gv_play.SelectedIndex -= 1;
        }

        private async void MTC_SendDanmakued(object sender, EventArgs e)
        {
            if (!ApiHelper.IsLogin() && !await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录!", 3000);
            }
            hidePointerFlag = true;
            mediaElement.Pause();
            SendDanmakuDialog dialog = new SendDanmakuDialog(playNow.Aid, playNow.Mid, mediaElement.Position.TotalSeconds);
            dialog.DanmakuSended += new EventHandler<SendDanmakuModel>((obj, item) =>
            {

                if (item.location == 1)
                {
                    danmu.AddRollDanmu(new NSDanmaku.Model.DanmakuModel { text = item.text, color = item.color.ToColor(), size = 25 }, true);
                }
                if (item.location == 4)
                {
                    danmu.AddBottomDanmu(new NSDanmaku.Model.DanmakuModel { text = item.text, color = item.color.ToColor(), size = 25 }, true);
                }
                if (item.location == 5)
                {
                    danmu.AddTopDanmu(new NSDanmaku.Model.DanmakuModel { text = item.text, color = item.color.ToColor(), size = 25 }, true);
                }
                mediaElement.Play();
            });
            await dialog.ShowAsync();
            hidePointerFlag = false;
            mediaElement.Play();
        }

        private void MTC_ShareEvent(object sender, EventArgs e)
        {
            Utils.SetClipboard(string.Format("http://www.bilibili.com/video/av{0}", playNow.Aid));
            Utils.ShowMessageToast("已将内容复制到剪切板", 3000);
        }

        private async void MTC_CoinsEvent(object sender, EventArgs e)
        {
            if (SettingHelper.IsPc())
            {
                MessageDialog messageDialog = new MessageDialog("确定要投币吗?", "投币");
                messageDialog.Commands.Add(new UICommand("投币X1", (com) => { TouBi(1); }, "1"));
                messageDialog.Commands.Add(new UICommand("投币X2", (com) => { TouBi(2); }, "2"));
                messageDialog.Commands.Add(new UICommand("取消", (com) => { }, "0"));
                await messageDialog.ShowAsync();
            }
            else
            {
                MenuFlyout menuFlyout = new MenuFlyout();
                var menu1 = new MenuFlyoutItem() { Text = "投币X1" };
                menu1.Click += new RoutedEventHandler((x, y) => { TouBi(1); });
                var menu2 = new MenuFlyoutItem() { Text = "投币X2" };
                menu2.Click += new RoutedEventHandler((x, y) => { TouBi(2); });
                menuFlyout.Items.Add(menu1);
                menuFlyout.Items.Add(menu2);

                menuFlyout.ShowAt(sender as AppBarButton);
            }

        }
        public async void TouBi(int num)
        {
            if (!ApiHelper.IsLogin() && !await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录!", 3000);
            }

            try
            {
                WebClientClass wc = new WebClientClass();
                Uri ReUri = new Uri("https://app.bilibili.com/x/v2/view/coin/add");
                string QuStr = string.Format("access_key={0}&aid={1}&appkey={2}&build=540000&from=7&mid={3}&platform=android&&multiply={4}&ts={5}", ApiHelper.access_key, playNow.Aid, ApiHelper._appKey, ApiHelper.GetUserId(), num, ApiHelper.GetTimeSpan);
                QuStr += "&sign=" + ApiHelper.GetSign(QuStr);
                string result = await WebClientClass.PostResults(ReUri, QuStr);
                JObject jObject = JObject.Parse(result);
                if (Convert.ToInt32(jObject["code"].ToString()) == 0)
                {
                    Utils.ShowMessageToast("投币成功！", 3000);
                }
                else
                {
                    Utils.ShowMessageToast(jObject["message"].ToString(), 3000);
                }
            }
            catch (Exception ex)
            {
                Utils.ShowMessageToast("投币时发生错误\r\n" + ex.Message, 3000);
            }

        }

        private void cb_Style_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_Style.SelectedIndex == -1 || danmu == null)
            {
                return;
            }
            danmu.borderStyle = (NSDanmaku.Model.DanmakuBorderStyle)cb_Style.SelectedIndex;
            SettingHelper.Set_DMStyle(cb_Style.SelectedIndex);

        }

        private void sw_DanmuNotSubtitle_Toggled(object sender, RoutedEventArgs e)
        {
            if (danmu == null)
            {
                return;
            }
            danmu.notHideSubtitle = sw_DanmuNotSubtitle.IsOn;
            SettingHelper.Set_DanmuNotSubtitle(sw_DanmuNotSubtitle.IsOn);

        }

        private void slider_Rate_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (mediaElement == null)
            {
                return;
            }
            mediaElement.PlaybackRate = slider_Rate.Value;
        }

        private void MTC_FullWindows(object sender, EventArgs e)
        {
            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
        }

        private void MTC_ExitFullWindows(object sender, EventArgs e)
        {
            ApplicationView.GetForCurrentView().ExitFullScreenMode();
        }

        private void menuitem_Capture_Click(object sender, RoutedEventArgs e)
        {
            CaptureVideo();
        }
        private async void CaptureVideo()
        {
            try
            {
                MTC.Visibility = Visibility.Collapsed;
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".jpg";
                StorageFolder applicationFolder = KnownFolders.PicturesLibrary;
                StorageFolder folder = await applicationFolder.CreateFolderAsync("bilibili UWP", CreationCollisionOption.OpenIfExists);
                StorageFile saveFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
                RenderTargetBitmap bitmap = new RenderTargetBitmap();
                await bitmap.RenderAsync(mediaElement);
                var pixelBuffer = await bitmap.GetPixelsAsync();
                using (var fileStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Ignore,
                         (uint)bitmap.PixelWidth,
                         (uint)bitmap.PixelHeight,
                         DisplayInformation.GetForCurrentView().LogicalDpi,
                         DisplayInformation.GetForCurrentView().LogicalDpi,
                         pixelBuffer.ToArray());
                    await encoder.FlushAsync();
                }
                Utils.ShowMessageToast("截图已经保存至图片库");
                MTC.Visibility = Visibility.Visible;
            }
            catch (Exception)
            {
                Utils.ShowMessageToast("截图失败");
            }

        }

        private void MTC_Captured(object sender, EventArgs e)
        {
            CaptureVideo();
        }

        private void MTC_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            MTC.HideOrShowMTC();
        }

        private void MTC_FastForward(object sender, double e)
        {
            mediaElement.Position.Add(TimeSpan.FromSeconds(e));
        }


        private void mediaElement_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
            _PointerHideTime = 1;
        }

        private void sw_BoldDanmu_Toggled(object sender, RoutedEventArgs e)
        {
            if (danmu == null)
            {
                return;
            }
            danmu.bold = sw_BoldDanmu.IsOn;
            SettingHelper.Set_BoldDanmu(sw_BoldDanmu.IsOn);
        }

        private async void menuitem_LocalDanmu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Windows.Storage.Pickers.FileOpenPicker fileOpenPicker = new Windows.Storage.Pickers.FileOpenPicker();
                fileOpenPicker.FileTypeFilter.Add(".xml");
                var file = await fileOpenPicker.PickSingleFileAsync();
                if (file != null)
                {
                    DanmakuParse danmakuParse = new DanmakuParse();
                    var ls = await danmakuParse.ParseBiliBili(file);
                    DanMuPool.AddRange(ls);
                }
            }
            catch (Exception)
            {
                Utils.ShowMessageToast("加载失败");
            }
           

        }

        private async void menuitem_tantan_Click(object sender, RoutedEventArgs e)
        {
            NSDanmaku.Controls.TantanDialog tantanDialog = new NSDanmaku.Controls.TantanDialog();
            tantanDialog.ReturnDanmakus += TantanDialog_ReturnDanmakus;
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
            hidePointerFlag = true;
            await tantanDialog.ShowAsync();
            hidePointerFlag = false;
        }

        private void TantanDialog_ReturnDanmakus(object sender, List<NSDanmaku.Model.DanmakuModel> e)
        {
            DanMuPool.AddRange(e);
        }
    }






}
