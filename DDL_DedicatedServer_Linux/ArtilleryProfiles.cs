namespace DakkaDataLink_Dedicated
{
    public class ArtilleryProfiles
    {
        private static ArtilleryProfiles? m_Instance = null;
        public static ArtilleryProfiles Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new ArtilleryProfiles();
                }
                return m_Instance;
            }

            private set { }
        }

        private ArtilleryProfiles()
        {
            ArtyProfilesDict = new Dictionary<string, ArtilleryProfile>
            {
                { Lariat120mm.ProfileName, Lariat120mm },
                { Exalt150mm.ProfileName, Exalt150mm },
                { Flood150mmSpg.ProfileName, Flood150mmSpg },
                { Thunderbolt150mm.ProfileName, Thunderbolt150mm },
                { Skycaller.ProfileName, Skycaller },
                { WaspNest.ProfileName, WaspNest },
                { Cremari_Mortar.ProfileName, Cremari_Mortar },
                { Mortar_Light_Tank.ProfileName, Mortar_Light_Tank },
                { Blacksteele_Frigate.ProfileName, Blacksteele_Frigate },
            };
            CurrentProfile = ArtyProfilesDict["Lariat 120mm Gun"];
        }
        public ArtilleryProfile CurrentProfile { get; set; }

    //    public void SwitchToProfile(string profileName)
    //    {
    //        switch (profileName)
    //        {
    //            case LARIAT_120_NAME:
    //                CurrentProfile = Lariat120mm;
    //                break;
    //
    //            case EXALT_150_NAME:
    //                CurrentProfile = Exalt150mm;
    //                break;
    //
    //            case SKYCALLER_NAME:
    //                CurrentProfile = Skycaller;
    //                break;
    //
    //            default:
    //                //Console.WriteLine($"ArtilleryProfiles.SwitchToProfile: Invalid profile name received: {profileName}");
    //                CurrentProfile = Lariat120mm;
    //                break;
    //        }
    //    }

        public Dictionary<string, ArtilleryProfile>ArtyProfilesDict { get; set; }

        public readonly ArtilleryProfile Lariat120mm = new ArtilleryProfile(LARIAT_120_NAME, 8.0, 100.0, 300.0);
        public readonly ArtilleryProfile Exalt150mm = new ArtilleryProfile(EXALT_150_NAME, 8.0, 100.0, 300.0);
        public readonly ArtilleryProfile Flood150mmSpg = new ArtilleryProfile(FLOOD_SPG_150_NAME, 7.6, 120.0, 250.0);
        public readonly ArtilleryProfile Thunderbolt150mm = new ArtilleryProfile(THUNDERBOLT_150_NAME, 6.0, 200.0, 350.0);
        public readonly ArtilleryProfile Skycaller = new ArtilleryProfile(SKYCALLER_NAME, 15.0, 200.0, 275.0);
        public readonly ArtilleryProfile WaspNest = new ArtilleryProfile(WASP_NEST_NAME, 10.0, 225.0, 325.0);
        public readonly ArtilleryProfile Cremari_Mortar = new ArtilleryProfile(CREMARI_MORTAR_NAME, 0.5, 45.0, 80.0);
        public readonly ArtilleryProfile Mortar_Light_Tank = new ArtilleryProfile(MORTAR_LIGHT_TANK_NAME, 2.3, 45.0, 80.0);
        public readonly ArtilleryProfile Blacksteele_Frigate = new ArtilleryProfile(BLACKSTEELE_FRIGATE_NAME, 4.5, 100.0, 200.0);

        public const string LARIAT_120_NAME = "Lariat 120mm Gun";
        public const string EXALT_150_NAME = "Exalt 150mm Gun (Warden)";
        public const string FLOOD_SPG_150_NAME = "Flood 150mm SPG";
        public const string THUNDERBOLT_150_NAME = "Thunderbolt 150mm Gun (Colonial)";
        public const string SKYCALLER_NAME = "Skycaller (Rocket Halftrack)";
        public const string WASP_NEST_NAME = "Wasp Nest (Towed Rocket)";
        public const string CREMARI_MORTAR_NAME = "Cremari Mortar";
        public const string MORTAR_LIGHT_TANK_NAME = "Devitt Mortar Tank (MLT)";
        public const string BLACKSTEELE_FRIGATE_NAME = "Blacksteele Frigate";

        public class ArtilleryProfile
        {
            public readonly string ProfileName;
            public readonly double DistTickSize;
            public readonly double MinDist;
            public readonly double MaxDist;

            internal ArtilleryProfile(string _profileName, double _distTickSize, double _minDist, double _maxDist)
            {
                ProfileName = _profileName;
                DistTickSize = _distTickSize;
                MinDist = _minDist;
                MaxDist = _maxDist;
            }
        }
    }
}
