using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.ModernUIPack;
using System.IO;
using System;
using IO.Swagger.Model;

namespace Base {
    public class NotificationsHololens : Notifications {
        public override void SaveLogs(Scene scene, Project project, string customNotificationTitle = "") {
            //
        }

        public override void ShowNotification(string title, string text) {
            Debug.Log(text);
        }
    }
}
