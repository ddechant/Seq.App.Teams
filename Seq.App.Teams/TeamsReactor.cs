﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Seq.Apps;
using Seq.Apps.LogEvents;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Seq.App.Teams
{
    [SeqApp("Teams",
    Description = "Sends log events to Teams.")]
    public class TeamsReactor : Reactor
    {
        
        private static IDictionary<LogEventLevel, string> _levelColorMap = new Dictionary<LogEventLevel, string>
        {
            {LogEventLevel.Verbose, "gray"},
            {LogEventLevel.Debug, "gray"},
            {LogEventLevel.Information, "green"},
            {LogEventLevel.Warning, "yellow"},
            {LogEventLevel.Error, "red"},
            {LogEventLevel.Fatal, "red"},
        };

        [SeqAppSetting(
        DisplayName = "Seq Base URL",
        HelpText = "Used for generating perma links to events in Teams messages.",
        IsOptional = true)]
        public string BaseUrl { get; set; }
        
        [SeqAppSetting(
        DisplayName = "Teams WebHook URL",
        HelpText = "Used to send message to Teams")]
        public string TeamsBaseUrl { get; set; }


        [SeqAppSetting(
        HelpText = "Background color for message. One of \"yellow\", \"red\", \"green\", \"purple\", \"gray\", or \"random\". (default: auto based on message level)",
        IsOptional = true)]
        public string Color { get; set; }

        [SeqAppSetting(
        HelpText = "Whether or not messages should trigger notifications for people in the room (change the tab color, play a sound, etc). Each recipient's notification preferences are taken into account.",
        IsOptional = true)]
        public bool Notify { get; set; }

        public async Task On(Event<LogEventData> evt)
        {
            using (var client = new HttpClient())
            {
                var url = TeamsBaseUrl;
                client.BaseAddress = new Uri(url);

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                TeamsCard body = BuildBody(evt);

                var response = await client.PostAsJsonAsync(
                    "",
                    body);

                if (!response.IsSuccessStatusCode)
                {
                    Log
                        .ForContext("Uri", response.RequestMessage.RequestUri)
                        .Error("Could not send Teams message, server replied {StatusCode} {StatusMessage}: {Message}", Convert.ToInt32(response.StatusCode), response.StatusCode, await response.Content.ReadAsStringAsync());
                }
            }
        }

        private TeamsCard BuildBody(Event<LogEventData> evt)
        {

            var msg = new StringBuilder("**" + evt.Data.Level.ToString() + ":** " + evt.Data.RenderedMessage);
            //if (msg.Length > 1000)
            //{
            //    msg.Length = 1000;
            //}


            TeamsPotentialAction action = new TeamsPotentialAction()
            {
                Type = "ViewAction",
                Name = "Click here to open in Seq",
                Target = new string[] { string.Format("{0}/#/events?filter=@Id%20%3D%3D%20%22{1}%22&show=expanded", BaseUrl, evt.Id) }
            };
            

            var color = Color;
            if (string.IsNullOrWhiteSpace(color))
            {
                color = _levelColorMap[evt.Data.Level];
            }

            TeamsCard body = new TeamsCard()
            {
                Title = "<span style='color:"+color+"'>" + evt.Data.Level.ToString()+"</span>",
                ThemeColor = color,
                Text = msg.ToString(),
                PotentialAction = new TeamsPotentialAction[] { action }
            };

            return body;
        }
    }
}
