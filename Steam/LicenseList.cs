﻿/*
 * Copyright (c) 2013-2015, SteamDB. All rights reserved.
 * Use of this source code is governed by a BSD-style license that can be
 * found in the LICENSE file.
 */
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using SteamKit2;

namespace SteamDatabaseBackend
{
    class LicenseList : SteamHandler
    {
        public LicenseList(CallbackManager manager)
            : base(manager)
        {
            manager.Register(new Callback<SteamApps.LicenseListCallback>(OnLicenseListCallback));
        }

        private static void OnLicenseListCallback(SteamApps.LicenseListCallback licenseList)
        {
            if (licenseList.Result != EResult.OK)
            {
                Log.WriteError("LicenseList", "Failed: {0}", licenseList.Result);

                return;
            }

            Log.WriteInfo("LicenseList", "Received {0} licenses from Steam", licenseList.LicenseList.Count);

            if (!licenseList.LicenseList.Any())
            {
                Application.Instance.OwnedSubs.Clear();
                Application.Instance.OwnedApps.Clear();

                return;
            }

            var ownedSubs = new Dictionary<uint, byte>();
            var ownedApps = new Dictionary<uint, byte>();

            foreach (var license in licenseList.LicenseList)
            {
                // For some obscure reason license list can contain duplicates
                if (Application.Instance.OwnedSubs.ContainsKey(license.PackageID))
                {
                    Log.WriteError("LicenseList", "Already contains {0} ({1})", license.PackageID, license.PaymentMethod);

                    continue;
                }

                ownedSubs.Add(license.PackageID, (byte)license.PaymentMethod);
            }

            using (MySqlDataReader Reader = DbWorker.ExecuteReader(string.Format("SELECT DISTINCT `AppID` FROM `SubsApps` WHERE `SubID` IN ({0})", string.Join(", ", ownedSubs.Keys))))
            {
                while (Reader.Read())
                {
                    ownedApps.Add(Reader.GetUInt32("AppID"), 1);
                }
            }

            Application.Instance.OwnedSubs = ownedSubs;
            Application.Instance.OwnedApps = ownedApps;
        }
    }
}