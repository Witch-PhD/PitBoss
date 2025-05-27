namespace DakkaDataLink
{
    internal class SpotterKeystrokeHandler
    {
        DisplayManager dataManager = DisplayManager.Instance;
        ArtilleryProfiles artyProfiles = ArtilleryProfiles.Instance;
        private bool m_IsActive = false;
        private bool m_SetBindingInProgress = false;

        bool rightCtrlHeldDown = false;
        bool rightShiftHeldDown = false;

        double smallDistIncrement = 1.0;
        double largeDistIncrement = 8.0;

        double smallAzIncrement = 1.0;
        double largeAzIncrement = 10.0;

        double azIncrement;
        double distIncrement;

        private static SpotterKeystrokeHandler? m_instance;
        public static SpotterKeystrokeHandler Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new SpotterKeystrokeHandler();
                }
                return m_instance;
            }
            private set { }
        }

        SpotterKeystrokeHandler()
        {
            azIncrement = smallAzIncrement;
            distIncrement = smallDistIncrement;
            currentKeys = new KeyCombo();
        }

        public void Activate()
        {
            m_IsActive = true;
        }

        public void Deactivate()
        {
            m_IsActive = false;
        }

        KeyCombo? currentKeys;
        private string keyBindingBeingSet = "";
        public void BeginSetKeyBinding(string bindingDictKeyName)
        {
            if (dataManager.userOptions.BindingDictionary.ContainsKey(bindingDictKeyName))
            {
                keyBindingBeingSet = bindingDictKeyName;
                currentKeys = new KeyCombo();
                m_SetBindingInProgress = true;
                m_IsActive = true;
            }
            else
            {
                throw new ArgumentException("BeginSetKeyBinding invalid args");
            }
        }

        public void EndSetKeyBinding()
        {
            m_SetBindingInProgress = false;
            m_IsActive = false;
            if (currentKeys.theComboList.Count >0)
            {
                dataManager.userOptions.BindingDictionary[keyBindingBeingSet] = currentKeys;
                currentKeys = new KeyCombo();
            }
        }

        
        internal void handleKeyDown(RawKeyEventArgs args)
        {
            if (!m_IsActive)
            {
                return;
            }
            // //Console.WriteLine("Key Down: " + args.Key.ToString());
            //////// Modifiers ////////

            if (m_SetBindingInProgress)
            {
                if (!(currentKeys.theComboList.Contains(args.Key)))
                {
                    currentKeys.theComboList.Add(args.Key);
                }
                return;
            }

            if (!(currentKeys.theComboList.Contains(args.Key)))
            {
                currentKeys.theComboList.Add(args.Key);
            }

            foreach (KeyValuePair<string, KeyCombo> validEntry in dataManager.userOptions.BindingDictionary)
            {
                if (validEntry.Value == currentKeys)
                {
                    switch (validEntry.Key)
                    {
                        case UserOptions.AZ_UP_ONE_DEG_DICT_KEY:
                            dataManager.LatestAz += 1.0;
                            break;

                        case UserOptions.AZ_UP_MULTI_DEG_DICT_KEY:
                            dataManager.LatestAz += 10.0;
                            break;

                        case UserOptions.AZ_DOWN_ONE_DEG_DICT_KEY:
                            dataManager.LatestAz -= 1.0;
                            break;

                        case UserOptions.AZ_DOWN_MULTI_DEG_DICT_KEY:
                            dataManager.LatestAz -= 10.0;
                            break;

                        case UserOptions.DIST_UP_ONE_TICK_DICT_KEY:
                            dataManager.LatestDist += artyProfiles.CurrentProfile.DistTickSize;
                            break;

                        case UserOptions.DIST_UP_MULTI_TICK_DICT_KEY:
                            dataManager.LatestDist += artyProfiles.CurrentProfile.DistTickSize * 3.0;
                            break;

                        case UserOptions.DIST_DOWN_ONE_TICK_DICT_KEY:
                            dataManager.LatestDist -= artyProfiles.CurrentProfile.DistTickSize;
                            break;

                        case UserOptions.DIST_DOWN_MULTI_TICK_DICT_KEY:
                            dataManager.LatestDist -= artyProfiles.CurrentProfile.DistTickSize * 3.0;
                            break;

                        case UserOptions.SEND_ARTY_MSG_DICT_KEY:
                            dataManager.SendCoords();
                            break;

                        default:

                            break;

                    }
                }
            }

        //    if (args.Key == Key.RightShift)
        //    {
        //        rightShiftHeldDown = true;
        //    }
        //
        //    else if (args.Key == Key.RightCtrl)
        //    {
        //        rightCtrlHeldDown = true;
        //    }
        //
        //    if (rightShiftHeldDown)
        //    {
        //        azIncrement = largeAzIncrement;
        //        distIncrement = artyProfiles.CurrentProfile.DistTickSize * 3.0;
        //    }
        //    else if (rightCtrlHeldDown)
        //    {
        //        azIncrement = smallAzIncrement;
        //        distIncrement = artyProfiles.CurrentProfile.DistTickSize;
        //    }
        //    else
        //    {
        //        return;
        //    }
        //
        //    // TODO: Make this a switch statement.
        //
        //    //////// Distance ////////
        //    if (args.Key == Key.Up)
        //    {
        //        displayManager.LatestDist += distIncrement;
        //    }
        //    else if (args.Key == Key.Down)
        //    {
        //        displayManager.LatestDist -= distIncrement;
        //    }
        //
        //    //////// Azimuth ////////
        //    else if (args.Key == Key.Right)
        //    {
        //        displayManager.LatestAz += azIncrement;
        //    }
        //    else if (args.Key == Key.Left)
        //    {
        //        displayManager.LatestAz -= azIncrement;
        //    }
        //
        //    //////// Send New Coords ////////
        //    else if (args.Key == Key.NumPad0)
        //    {
        //        if (rightCtrlHeldDown)
        //        {
        //            displayManager.SendCoords();
        //        }
        //    }

        }

        internal void handleKeyUp(RawKeyEventArgs args)
        {
            if ((!m_IsActive) || (m_SetBindingInProgress))
            {
                return;
            }
           // //Console.WriteLine("Key Up: " + args.Key.ToString());
            //    if (args.Key == Key.RightShift)
            //    {
            //        rightShiftHeldDown = false;
            //    }
            //    else if (args.Key == Key.RightCtrl)
            //    {
            //        rightCtrlHeldDown = false;
            //    }
            currentKeys.theComboList.Remove(args.Key);
        }
    }
}
