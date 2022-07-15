using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.Linq;

namespace LoliPoliceDepartment.Utilities
{
    public class GunsmithUtility : EditorWindow
    {
        Vector2 scroll;

        Texture2D HeaderTexture;
        VRCAvatarDescriptor Avatar;
        RuntimeAnimatorController FXAnimator;
        VRCExpressionParameters Expressions;
        string LayerName = "";
        int Ammo = 0;
        bool FullAuto = false;
        bool selectfire = false;
        bool ChamberCheck = false;
        bool MagCheck = false;
        string newCustomAnimationSetName;
        List<CustomAnimations> CustomAnimations = new List<CustomAnimations>();

        //setup for all gestures to handle transitions
        public enum Gestures
        {
            RightNeutral,
            LeftNeutral,
            RightFist,
            LeftFist,
            RightHandOpen,
            LeftHandOpen,
            RightFingerPoint,
            LeftFingerPoint,
            RightVictory,
            LeftVictory,
            RightRockNRoll,
            LeftRockNRoll,
            RightHandGun,
            LeftHandGun,
            RightThumbsUp,
            LeftThumbsUp,
        }
        Gestures FireGesture;
        Gestures ReloadGesture;
        Gestures ChamberCheckGesture;
        Gestures MagCheckGesture;

        AnimatorController Controller;
        int layerIndex = -1;

        //Required Animations
        AnimationClip FireAnim;
        AnimationClip FullReloadAnim;
        //Optional Animations
        AnimationClip TacReloadAnim;
        AnimationClip EmptyStateAnim;
        AnimationClip EmptyClickAnim;
        AnimationClip FinalShotAnim;
        //Chamber Check animations
        AnimationClip ChamberCheckPullAnim;
        AnimationClip ChamberCheckReleaseAnim;
        //Mag Check Animations
        AnimationClip MagCheckOutAnim;
        AnimationClip MagCheckInAnim;

        [MenuItem("LPD/Gunsmith")]
        public static void ShowWindow()
        {
            GunsmithUtility window = (GunsmithUtility)GetWindow<GunsmithUtility>("Gunsmith");
            window.maxSize = new Vector2(1024f, 4000);
            window.minSize = new Vector2(256, 512);
            window.Show();
        }

        public void OnEnable()
        {
            HeaderTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/LPD/Gunsmith/Resources/TITLEBAR.png", typeof(Texture2D));
        }

        private void OnGUI()
        {
            float drawarea = Screen.width / 4;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, drawarea), HeaderTexture, ScaleMode.ScaleToFit);
            
            GUILayout.BeginArea(new Rect(0, drawarea, Screen.width, Screen.height));
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight((Screen.height - drawarea) - 45));
            GUILayout.Space(5f);

            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Selected Avatar", EditorStyles.boldLabel);
                Avatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(Avatar, typeof(VRCAvatarDescriptor), true, GUILayout.Width(Screen.width / 2));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Enter layer name");
                LayerName = GUILayout.TextField(LayerName, GUILayout.Width(Screen.width/2));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Enter ammo count");
                Ammo = EditorGUILayout.IntField(Ammo, GUILayout.Width(Screen.width / 2));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Full Auto");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                FullAuto = GUILayout.Toggle(FullAuto,"");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                EditorGUI.BeginDisabledGroup(!FullAuto);
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select Fire");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                selectfire = GUILayout.Toggle(selectfire, "");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                EditorGUI.EndDisabledGroup();

                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Chamber Check");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                ChamberCheck = GUILayout.Toggle(ChamberCheck, "");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Mag Check");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                MagCheck = GUILayout.Toggle(MagCheck, "");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                GUILayout.Space(5f);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Custom Animation Stacks", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Name");
                newCustomAnimationSetName = GUILayout.TextField(newCustomAnimationSetName);
                if (GUILayout.Button("Add Custom Animation Stack"))
                {
                    if (newCustomAnimationSetName != "")
                    {
                        CustomAnimations newCustomAnimation = new CustomAnimations();
                        newCustomAnimation.Name = newCustomAnimationSetName;
                        CustomAnimation Entry = new CustomAnimation()
                        {
                            Animation = null,
                            transitions = new List<CustomAnimation.TransitionMethod>() { CustomAnimation.TransitionMethod.Gesture},
                            gesture = new List<Gestures>() { Gestures.LeftFist },
                            ParameterNames = new List<string>() { "Parameter" },
                            ParameterValues = new List<float>() { 0f },
                            conditionmode = new List<AnimatorConditionMode>() { AnimatorConditionMode.Equals },

                        };
                        newCustomAnimation.Animations.Add(Entry);
                        CustomAnimations.Add(newCustomAnimation);
                    }
                   else Debug.LogWarning("<color=#e0115fff><b>Gunsmith:</b></color> A name is required to add a custom animation set");
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(3f);

                for (int i = 0; i < CustomAnimations.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(CustomAnimations[i].Name);
                    if (GUILayout.Button("Remove"))
                    {
                        CustomAnimations.Remove(CustomAnimations[i]);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }

            GUILayout.Space(5f);

            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Core Animations", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(3f);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Fire Animation");
                FireAnim = (AnimationClip)EditorGUILayout.ObjectField(FireAnim, typeof(AnimationClip), true, GUILayout.Width(Screen.width / 2));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Full Reload Animation");
                FullReloadAnim = (AnimationClip)EditorGUILayout.ObjectField(FullReloadAnim, typeof(AnimationClip), true, GUILayout.Width(Screen.width / 2));
                GUILayout.EndHorizontal();

                GUILayout.Space(5f);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Optional Animations", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(3f);
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("Tactical Reload Animation");
                TacReloadAnim = (AnimationClip)EditorGUILayout.ObjectField(TacReloadAnim, typeof(AnimationClip), true, GUILayout.Width(Screen.width / 2));
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("Empty State Animation");
                EmptyStateAnim = (AnimationClip)EditorGUILayout.ObjectField(EmptyStateAnim, typeof(AnimationClip), true, GUILayout.Width(Screen.width / 2));
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("Empty Click Animation");
                EmptyClickAnim = (AnimationClip)EditorGUILayout.ObjectField(EmptyClickAnim, typeof(AnimationClip), true, GUILayout.Width(Screen.width / 2));
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("Final Shot Animation");
                FinalShotAnim = (AnimationClip)EditorGUILayout.ObjectField(FinalShotAnim, typeof(AnimationClip), true, GUILayout.Width(Screen.width / 2));
                GUILayout.EndHorizontal();

                GUILayout.Space(5f);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Gestures", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(3f);

                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                GUILayout.Label("Fire Gesture");
                FireGesture = (Gestures)EditorGUILayout.EnumPopup(FireGesture);
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                GUILayout.Label("Reload Gesture");
                ReloadGesture = (Gestures)EditorGUILayout.EnumPopup(ReloadGesture);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            
            if (ChamberCheck)
            {
                GUILayout.Space(5f);
                using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.BeginVertical();

                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Chamber Check", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.Space(3f);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Pull Animation");
                    ChamberCheckPullAnim = (AnimationClip)EditorGUILayout.ObjectField(ChamberCheckPullAnim, typeof(AnimationClip), true, GUILayout.Width(Screen.width / 2));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Release Animation");
                    ChamberCheckReleaseAnim = (AnimationClip)EditorGUILayout.ObjectField(ChamberCheckReleaseAnim, typeof(AnimationClip), true, GUILayout.Width(Screen.width / 2));
                    GUILayout.EndHorizontal();

                    GUILayout.Space(3f);
                    GUILayout.Label("Gesture");
                    ChamberCheckGesture = (Gestures)EditorGUILayout.EnumPopup(ChamberCheckGesture);
                    GUILayout.EndVertical();
                }
            }

            if (MagCheck)
            {
                GUILayout.Space(5f);
                using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.BeginVertical();

                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Mag Check", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.Space(3f);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Mag Out Animation");
                    MagCheckOutAnim = (AnimationClip)EditorGUILayout.ObjectField(MagCheckOutAnim, typeof(AnimationClip), true, GUILayout.Width(Screen.width / 2));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Mag In Animation");
                    MagCheckInAnim = (AnimationClip)EditorGUILayout.ObjectField(MagCheckInAnim, typeof(AnimationClip), true, GUILayout.Width(Screen.width / 2));
                    GUILayout.EndHorizontal();

                    GUILayout.Space(3f);
                    GUILayout.Label("Gesture");
                    MagCheckGesture = (Gestures)EditorGUILayout.EnumPopup(MagCheckGesture);
                    GUILayout.EndVertical();
                }
            }

            for (int i = 0; i < CustomAnimations.Count; i++)
            {
                GUILayout.Space(5f);
                using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.BeginVertical();

                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(CustomAnimations[i].Name, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.Space(3f);

                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Entry Point");
                    CustomAnimations[i].EntryPoint = (AnimationClip)EditorGUILayout.ObjectField(CustomAnimations[i].EntryPoint, typeof(AnimationClip), true);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    for (int j = 0; j < CustomAnimations[i].Animations.Count; j++)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("↓");
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        if (GUILayout.Button("Remove Animation", GUILayout.Width(150)))
                        {
                            CustomAnimations[i].Animations.RemoveAt(j);
                            continue;
                        }
                        for (int k = 0; k < CustomAnimations[i].Animations[j].transitions.Count; k++)
                        {
                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button("X", GUILayout.Width(25)))
                            {
                                CustomAnimations[i].Animations[j].transitions.RemoveAt(k);
                                CustomAnimations[i].Animations[j].conditionmode.RemoveAt(k);
                                CustomAnimations[i].Animations[j].gesture.RemoveAt(k);
                                CustomAnimations[i].Animations[j].ParameterNames.RemoveAt(k);
                                CustomAnimations[i].Animations[j].ParameterValues.RemoveAt(k);
                                continue;
                            }
                            GUILayout.Label("Condition " + k);
                            CustomAnimations[i].Animations[j].transitions[k] = (CustomAnimation.TransitionMethod)EditorGUILayout.EnumPopup(CustomAnimations[i].Animations[j].transitions[k]);
                            CustomAnimations[i].Animations[j].conditionmode[k] = (AnimatorConditionMode)EditorGUILayout.EnumPopup(CustomAnimations[i].Animations[j].conditionmode[k]);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            if (CustomAnimations[i].Animations[j].transitions[k] == CustomAnimation.TransitionMethod.Gesture)
                            {
                                CustomAnimations[i].Animations[j].gesture[k] = (Gestures)EditorGUILayout.EnumPopup(CustomAnimations[i].Animations[j].gesture[k]);
                            }
                            if (CustomAnimations[i].Animations[j].transitions[k] == CustomAnimation.TransitionMethod.Parameter)
                            {
                                GUILayout.Label("Parameter Name");
                                CustomAnimations[i].Animations[j].ParameterNames[k] = GUILayout.TextField(CustomAnimations[i].Animations[j].ParameterNames[k]);
                                GUILayout.Label("Parameter Value");
                                CustomAnimations[i].Animations[j].ParameterValues[k] = EditorGUILayout.FloatField(CustomAnimations[i].Animations[j].ParameterValues[k]);
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.Space(3);
                        }

                        if (GUILayout.Button("Add Condition", GUILayout.Width(Screen.width / 4)))
                        {
                            CustomAnimation.TransitionMethod newTrans = new CustomAnimation.TransitionMethod();
                            CustomAnimations[i].Animations[j].transitions.Add(newTrans);
                            Gestures newgesture = Gestures.LeftFist;
                            CustomAnimations[i].Animations[j].gesture.Add(newgesture);
                            string newParameterName = "Parameter";
                            CustomAnimations[i].Animations[j].ParameterNames.Add(newParameterName);
                            float newParameterValue = 0f;
                            CustomAnimations[i].Animations[j].ParameterValues.Add(newParameterValue);
                            AnimatorConditionMode newConditionMode = AnimatorConditionMode.Equals;
                            CustomAnimations[i].Animations[j].conditionmode.Add(newConditionMode);
                        }
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Animation");
                        CustomAnimations[i].Animations[j].Animation = (AnimationClip)EditorGUILayout.ObjectField(CustomAnimations[i].Animations[j].Animation, typeof(AnimationClip), true, GUILayout.Width(Screen.width / 2));
                        GUILayout.EndHorizontal();
                        GUILayout.Space(5f);
                    }
                    if (GUILayout.Button("Add Animation"))
                    {
                        CustomAnimation Entry = new CustomAnimation()
                        {
                            Animation = null,
                            transitions = new List<CustomAnimation.TransitionMethod>() { CustomAnimation.TransitionMethod.Gesture},
                            gesture = new List<Gestures>() { Gestures.LeftFist },
                            ParameterNames = new List<string>() { "Parameter" },
                            ParameterValues = new List<float>() { 0f },
                            conditionmode = new List<AnimatorConditionMode>() { AnimatorConditionMode.Equals },
                        };
                        CustomAnimations[i].Animations.Add(Entry);
                    }
                    GUILayout.EndVertical();
                }
            }

            GUILayout.Space(5f);
            if(GUILayout.Button("Generate Animation layer"))
            {
                if (ValidateFields())
                {
                    ValidateLayer();
                    GenerateCore();
                    if (CustomAnimations.Count > 0)
                    {
                        GenerateCustomAnimationSets();
                    }
                }
            }
            /*
            Controller = (AnimatorController)EditorGUILayout.ObjectField(Controller, typeof(AnimatorController));
            if (GUILayout.Button("Get Positions"))
            {
                layerIndex = GetLayerNumber(Controller);
                for (int i = 0; i < Controller.layers[layerIndex].stateMachine.states.Length; i++)
                {
                    Debug.Log(Controller.layers[layerIndex].stateMachine.states[i].state.name + " " + Controller.layers[layerIndex].stateMachine.states[i].position);
                }
            }*/
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
            GUILayout.BeginArea(new Rect(0, Screen.height - 43f, Screen.width, 25f));
            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Thank you for your support <3", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndArea();
        }

        private void ValidateLayer()
        {
            Controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(FXAnimator));
            layerIndex = GetLayerNumber(Controller);
            if (layerIndex == -1)
            {
                //layer doesnt exist. add it here
                AnimatorControllerLayer newLayer = new AnimatorControllerLayer
                {
                    name = LayerName,
                    defaultWeight = 1f,
                    stateMachine = new AnimatorStateMachine
                    {
                        name = LayerName,
                    }
            };
                
                Controller.AddLayer(newLayer);
                AssetDatabase.AddObjectToAsset(newLayer.stateMachine, AssetDatabase.GetAssetPath(FXAnimator));
                AssetDatabase.SaveAssets();

                layerIndex = Controller.layers.Length - 1;
                layerIndex = GetLayerNumber(Controller);
                Debug.Log("<color=#e0115fff><b>Gunsmith:</b></color> Created new layer " + layerIndex + " called " + LayerName);
            }
            else Debug.Log("<color=green><b>Gunsmith:</b></color> Found layer " + layerIndex + " called " + LayerName);
        }
        private void GenerateCore()
        {
            //check for final shot state
            bool hasFinalShot = FinalShotAnim;

            //add parameters
            List<AnimatorControllerParameter> ControllerParameters = Controller.parameters.ToList<AnimatorControllerParameter>();
            AnimatorControllerParameter AmmoCount = new AnimatorControllerParameter
            {
                name = LayerName + " Ammo Count",
                type = AnimatorControllerParameterType.Int,
                defaultInt = Ammo
            };
            ControllerParameters.Add(AmmoCount);
            Controller.parameters = ControllerParameters.ToArray();
       
            Controller.AddParameter(LayerName + " Ammo Empty", AnimatorControllerParameterType.Bool);
            if(selectfire) Controller.AddParameter(LayerName + " SelectFire", AnimatorControllerParameterType.Bool);
            Controller.AddParameter(LayerName + " Equipped", AnimatorControllerParameterType.Bool);
            Controller.AddParameter("IsLocal", AnimatorControllerParameterType.Bool);
            Debug.Log("<color=#e0115fff><b>Gunsmith:</b></color> Setup Parameters for " + LayerName);

            //add vrchat synced parameters
            List<VRCExpressionParameters.Parameter> parameters = Expressions.parameters.ToList();
            var newParam = new VRCExpressionParameters.Parameter
            {
                name = LayerName + " Ammo Empty",
                saved = true,
                defaultValue = 0,
                valueType = VRCExpressionParameters.ValueType.Bool
            };
            parameters.Add(newParam);

            newParam = new VRCExpressionParameters.Parameter
            {
                name = LayerName + " Equipped",
                saved = true,
                defaultValue = 0,
                valueType = VRCExpressionParameters.ValueType.Bool
            };
            parameters.Add(newParam);
            if (selectfire)
            {
                newParam = new VRCExpressionParameters.Parameter
                {
                    name = LayerName + " SelectFire",
                    saved = true,
                    defaultValue = 0,
                    valueType = VRCExpressionParameters.ValueType.Bool
                };
                parameters.Add(newParam);
            }
            Expressions.parameters = parameters.ToArray();
            EditorUtility.SetDirty(Expressions);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#e0115fff><b>Gunsmith:</b></color> Setup VRC Expressions for " + LayerName);

            //start constructing the animator
            //create all required states
            AnimatorState InitilizeState = Controller.layers[layerIndex].stateMachine.AddState("Initialize", new Vector3(30.0f, 190.0f, 0.0f));
            AnimatorState WeaponStowedState = Controller.layers[layerIndex].stateMachine.AddState("Weapon Stowed", new Vector3(30.0f, 260.0f, 0.0f));
            AnimatorState ReadyToShootState = Controller.layers[layerIndex].stateMachine.AddState("Ready to fire", new Vector3(30.0f, 410.0f, 0.0f));
            AnimatorState Firestate = Controller.layers[layerIndex].stateMachine.AddState(FireAnim.name, new Vector3(30.0f, 480.0f, 0.0f));
            AnimatorState DecrementAmmoState = Controller.layers[layerIndex].stateMachine.AddState("Decrement Ammo", new Vector3(-390.0f, 410.0f, 0.0f));
            DecrementAmmoState.writeDefaultValues = false;
            AnimatorState ForceSyncState = Controller.layers[layerIndex].stateMachine.AddState("Force Empty", new Vector3(-390.0f, 330.0f, 0.0f));
            AnimatorState AmmoEmptyState = Controller.layers[layerIndex].stateMachine.AddState("Ammo Empty", new Vector3(-390.0f, 260.0f, 0.0f));
            AnimatorState EmptyClickState = Controller.layers[layerIndex].stateMachine.AddState("EmptyClick", new Vector3(-390.0f, 190.0f, 0.0f));
            AnimatorState FullReloadState = Controller.layers[layerIndex].stateMachine.AddState(FullReloadAnim.name, new Vector3(-90.0f, 330.0f, 0.0f));
            AnimatorState TecticalReloadState;
            if (TacReloadAnim != null)
            {
                TecticalReloadState = Controller.layers[layerIndex].stateMachine.AddState(TacReloadAnim.name, new Vector3(300.0f, 410.0f, 0.0f));
                TecticalReloadState.motion = TacReloadAnim;
            }
            else
            {
                TecticalReloadState = Controller.layers[layerIndex].stateMachine.AddState("TacReload " + FullReloadAnim.name, new Vector3(300.0f, 410.0f, 0.0f));
                TecticalReloadState.motion = FullReloadAnim;
            }
            Debug.Log("<color=#e0115fff><b>Gunsmith:</b></color> Setup States for " + LayerName);

            //add required animations
            Firestate.motion = FireAnim;
            FullReloadState.motion = FullReloadAnim;
            
            //add optional animations
            AmmoEmptyState.motion = EmptyStateAnim;
            EmptyClickState.motion = EmptyClickAnim;

            //create transitions
            AnimatorStateTransition InitToStowedTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = WeaponStowedState
            };
            AnimatorStateTransition StowedToReadyTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = ReadyToShootState
            };
            AnimatorStateTransition ReadyToStowedTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = WeaponStowedState
            };
            AnimatorStateTransition ReadyToFireTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = Firestate
            };
            AnimatorStateTransition FireToReadyTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = ReadyToShootState
            };
            AnimatorStateTransition FireToDecrementTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = DecrementAmmoState
            };
            AnimatorStateTransition DecrementToReadyTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = ReadyToShootState
            };
            AnimatorStateTransition DecrementToForceSyncTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = ForceSyncState
            };
            AnimatorStateTransition ForceSyncToAmmoEmptyTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = AmmoEmptyState
            };
            AnimatorStateTransition FireToEmptyTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = AmmoEmptyState
            };
            AnimatorStateTransition AmmoEmptyToReloadTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = FullReloadState
            };
            
            AnimatorStateTransition FullReloadToReadyTrans = new AnimatorStateTransition
            {
                duration = .1f,
                exitTime = 1f,
                hasExitTime = true,
                destinationState = ReadyToShootState
            };
            AnimatorStateTransition ReadyToTacReloadTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = TecticalReloadState
            };
            AnimatorStateTransition TacReloadToReadyTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 1f,
                hasExitTime = true,
                destinationState = ReadyToShootState
            };
            AnimatorStateTransition StowedToEmptyTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = AmmoEmptyState
            };
            AnimatorStateTransition EmptyToStowedTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = WeaponStowedState
            };
            AnimatorStateTransition EmptyToEmptyClickTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = EmptyClickState
            };
            AnimatorStateTransition EmptyClickToEmptyTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = AmmoEmptyState
            };
            AnimatorStateTransition ReadyToEmptyTrans = new AnimatorStateTransition
            {
                duration = 0f,
                exitTime = 0f,
                hasExitTime = false,
                destinationState = AmmoEmptyState
            };
            //final shot stuff
            Debug.Log("<color=#e0115fff><b>Gunsmith:</b></color> Setup Transitions for " + LayerName);

            // create conditions
            string[] fire = GestureLookup(FireGesture);
            string[] reload = GestureLookup(ReloadGesture);

            //go from stow to ready
            StowedToReadyTrans.AddCondition(AnimatorConditionMode.If, 1, LayerName + " Equipped");  
            StowedToReadyTrans.AddCondition(AnimatorConditionMode.IfNot, 1, LayerName + " Ammo Empty");
            //go from stow to empty
            StowedToEmptyTrans.AddCondition(AnimatorConditionMode.If, 1, LayerName + " Equipped");
            StowedToEmptyTrans.AddCondition(AnimatorConditionMode.If, 1, LayerName + " Ammo Empty");
            //go from ready to stow
            ReadyToStowedTrans.AddCondition(AnimatorConditionMode.IfNot, 1, LayerName + " Equipped");
            //go from ready to tac reload
            ReadyToTacReloadTrans.AddCondition(AnimatorConditionMode.Equals, Int32.Parse(reload[1]), reload[0]);
            //go from ready to fire
            ReadyToFireTrans.AddCondition(AnimatorConditionMode.Equals, Int32.Parse(fire[1]), fire[0]);
            ReadyToFireTrans.AddCondition(AnimatorConditionMode.Greater, .9f, fire[0] + "Weight");
            //go from fire to ready
            FireToReadyTrans.AddCondition(AnimatorConditionMode.IfNot, 1, "IsLocal");
            FireToReadyTrans.AddCondition(AnimatorConditionMode.NotEqual, Int32.Parse(fire[1]), fire[0]);
            FireToReadyTrans.AddCondition(AnimatorConditionMode.IfNot, 1, LayerName + " Ammo Empty");
            //go from fire to decrement ammo
            FireToDecrementTrans.AddCondition(AnimatorConditionMode.If, 1, "IsLocal");
            FireToDecrementTrans.AddCondition(AnimatorConditionMode.NotEqual, Int32.Parse(fire[1]), fire[0]);
            //go from decrement to ready
            DecrementToReadyTrans.AddCondition(AnimatorConditionMode.NotEqual, Int32.Parse(fire[1]), fire[0]);
            if (hasFinalShot) DecrementToReadyTrans.AddCondition(AnimatorConditionMode.Greater, 1, LayerName + " Ammo Count");
            else DecrementToReadyTrans.AddCondition(AnimatorConditionMode.Greater, 0, LayerName + " Ammo Count");
            //go from decrement to force empty sync
            DecrementToForceSyncTrans.AddCondition(AnimatorConditionMode.Less, 1, LayerName + " Ammo Count");
            //go from force empty to empty state
            ForceSyncToAmmoEmptyTrans.AddCondition(AnimatorConditionMode.If, 1, LayerName + " Ammo Empty");
            //go from fire to empty
            FireToEmptyTrans.AddCondition(AnimatorConditionMode.If, 1, LayerName + " Ammo Empty");
            //go from ready to empty
            ReadyToEmptyTrans.AddCondition(AnimatorConditionMode.If, 1, LayerName + " Ammo Empty");
            //go from empty to empty click
            EmptyToEmptyClickTrans.AddCondition(AnimatorConditionMode.Equals, Int32.Parse(fire[1]), fire[0]);
            //go from empty click to empty
            EmptyClickToEmptyTrans.AddCondition(AnimatorConditionMode.NotEqual, Int32.Parse(fire[1]), fire[0]);
            //go from empty to stow
            EmptyToStowedTrans.AddCondition(AnimatorConditionMode.IfNot, 1, LayerName + " Equipped");
            //go from empty to full reload
            AmmoEmptyToReloadTrans.AddCondition(AnimatorConditionMode.Equals, Int32.Parse(reload[1]), reload[0]);

            Debug.Log("<color=#e0115fff><b>Gunsmith:</b></color> Setup Conditions for " + LayerName);

            //add transitions
            AssetDatabase.AddObjectToAsset(InitToStowedTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(ReadyToFireTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(FireToReadyTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(FireToDecrementTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(FireToEmptyTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(ReadyToTacReloadTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(ReadyToStowedTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(StowedToReadyTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(DecrementToReadyTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(DecrementToForceSyncTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(ForceSyncToAmmoEmptyTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(AmmoEmptyToReloadTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(FullReloadToReadyTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(EmptyToStowedTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(EmptyToEmptyClickTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(EmptyClickToEmptyTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(TacReloadToReadyTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(StowedToEmptyTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
            AssetDatabase.AddObjectToAsset(ReadyToEmptyTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));

            InitilizeState.AddTransition(InitToStowedTrans);
            WeaponStowedState.AddTransition(StowedToEmptyTrans);
            WeaponStowedState.AddTransition(StowedToReadyTrans);
            ReadyToShootState.AddTransition(ReadyToStowedTrans);
            ReadyToShootState.AddTransition(ReadyToFireTrans);
            ReadyToShootState.AddTransition(ReadyToEmptyTrans);
            ReadyToShootState.AddTransition(ReadyToTacReloadTrans);
            TecticalReloadState.AddTransition(TacReloadToReadyTrans);
            Firestate.AddTransition(FireToReadyTrans);
            Firestate.AddTransition(FireToDecrementTrans);
            Firestate.AddTransition(FireToEmptyTrans);
            DecrementAmmoState.AddTransition(DecrementToReadyTrans);
            DecrementAmmoState.AddTransition(DecrementToForceSyncTrans);
            ForceSyncState.AddTransition(ForceSyncToAmmoEmptyTrans);
            AmmoEmptyState.AddTransition(AmmoEmptyToReloadTrans);
            FullReloadState.AddTransition(FullReloadToReadyTrans);
            AmmoEmptyState.AddTransition(EmptyToStowedTrans);
            AmmoEmptyState.AddTransition(EmptyToEmptyClickTrans);
            EmptyClickState.AddTransition(EmptyClickToEmptyTrans);

            
            
            Debug.Log("<color=#e0115fff><b>Gunsmith:</b></color> Added Transitions to " + LayerName);
           
            EditorUtility.SetDirty(Controller);
            AssetDatabase.SaveAssets();

            // add parameter drivers
            VRCAvatarParameterDriver InitializeAmmoDriver = (VRCAvatarParameterDriver)InitilizeState.AddStateMachineBehaviour(typeof(VRCAvatarParameterDriver));
            VRCAvatarParameterDriver.Parameter InitAmmoCount = new VRCAvatarParameterDriver.Parameter
            {
                name = LayerName + " Ammo Count",
                type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set,
                value = Ammo,
            };
            InitializeAmmoDriver.parameters.Add(InitAmmoCount);
            VRCAvatarParameterDriver DecrementAmmoCountDriver = (VRCAvatarParameterDriver)DecrementAmmoState.AddStateMachineBehaviour(typeof(VRCAvatarParameterDriver));
            VRCAvatarParameterDriver.Parameter DecrementAmmo = new VRCAvatarParameterDriver.Parameter
            {
                name = LayerName + " Ammo Count",
                type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Add,
                value = -1,
            };
            DecrementAmmoCountDriver.parameters.Add(DecrementAmmo);

            VRCAvatarParameterDriver ResetAmmoCountDriver = (VRCAvatarParameterDriver)FullReloadState.AddStateMachineBehaviour(typeof(VRCAvatarParameterDriver));
            VRCAvatarParameterDriver.Parameter ResetAmmoCount = new VRCAvatarParameterDriver.Parameter
            {
                name = LayerName + " Ammo Count",
                type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set,
                value = Ammo,
            };
            ResetAmmoCountDriver.parameters.Add(ResetAmmoCount);

            VRCAvatarParameterDriver TacResetAmmoCountDriver = (VRCAvatarParameterDriver)TecticalReloadState.AddStateMachineBehaviour(typeof(VRCAvatarParameterDriver));
            TacResetAmmoCountDriver.parameters.Add(ResetAmmoCount);

            VRCAvatarParameterDriver.Parameter ResetAmmoBool = new VRCAvatarParameterDriver.Parameter
            {
                name = LayerName + " Ammo Empty",
                type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set,
                value = 0,
            };
            ResetAmmoCountDriver.parameters.Add(ResetAmmoBool);

            VRCAvatarParameterDriver AmmoEmptyDriver = (VRCAvatarParameterDriver)ForceSyncState.AddStateMachineBehaviour(typeof(VRCAvatarParameterDriver));
            VRCAvatarParameterDriver.Parameter AmmoEmpty = new VRCAvatarParameterDriver.Parameter
            {
                name = LayerName + " Ammo Empty",
                type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set,
                value = 1,
            };
            AmmoEmptyDriver.parameters.Add(AmmoEmpty);
            Debug.Log("<color=#e0115fff><b>Gunsmith:</b></color> Setup Parameter drivers for " + LayerName);

            //full auto aditional animmations
            if (FullAuto)
            {
                AnimatorState FullAutoState = Controller.layers[layerIndex].stateMachine.AddState(FireAnim.name + " Full Auto", new Vector3(-390.0f, 480.0f, 0.0f));
                FullAutoState.motion = FireAnim;

                AnimatorStateTransition DecrementTofullAuto = new AnimatorStateTransition
                {
                    duration = 0f,
                    exitTime = 0f,
                    hasExitTime = false,
                    destinationState = FullAutoState
                };

                AnimatorStateTransition FullAutoToFireTrans = new AnimatorStateTransition
                {
                    duration = .1f,
                    exitTime = 0f,
                    hasExitTime = true,
                    destinationState = Firestate
                };
                AnimatorStateTransition FireToFullAutoTrans = new AnimatorStateTransition
                {
                    duration = .1f,
                    exitTime = 0f,
                    hasExitTime = true,
                    destinationState = FullAutoState
                };
                AnimatorStateTransition FullAutoToReadyTrans = new AnimatorStateTransition
                {
                    duration = 0f,
                    exitTime = 0f,
                    hasExitTime = false,
                    destinationState = ReadyToShootState
                };
                if (selectfire)
                {
                    AnimatorStateTransition FireToDecrementFullAutoTrans = new AnimatorStateTransition()
                    {
                        duration = .1f,
                        exitTime = 0f,
                        hasExitTime = true,
                        destinationState = DecrementAmmoState
                    };
                    FireToDecrementFullAutoTrans.AddCondition(AnimatorConditionMode.If, 1, LayerName + " SelectFire");
                    FireToDecrementFullAutoTrans.AddCondition(AnimatorConditionMode.If, 1, "IsLocal");
                    FireToDecrementTrans.AddCondition(AnimatorConditionMode.IfNot, 1, LayerName + " SelectFire");
                    DecrementTofullAuto.AddCondition(AnimatorConditionMode.If, 1, LayerName + " SelectFire");
                    FireToFullAutoTrans.AddCondition(AnimatorConditionMode.If, 1, LayerName + " SelectFire");

                    AssetDatabase.AddObjectToAsset(FireToDecrementFullAutoTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
                    Firestate.AddTransition(FireToDecrementFullAutoTrans);
                    
                    newParam = new VRCExpressionParameters.Parameter
                    {
                        name = LayerName + " SelectFire",
                        saved = true,
                        defaultValue = 0,
                        valueType = VRCExpressionParameters.ValueType.Bool
                    };
                    parameters.Add(newParam);
                    EditorUtility.SetDirty(Expressions);
                }
                else
                {
                    //setup transition for full auto only
                    FireToDecrementTrans.duration = .1f;
                    FireToDecrementTrans.hasExitTime = true;
                    AnimatorCondition SemiAutoCondition = new AnimatorCondition()
                    {
                        parameter = fire[0],
                        threshold = Int32.Parse(fire[1]),
                        mode = AnimatorConditionMode.NotEqual
                    };
                    FireToDecrementTrans.RemoveCondition(SemiAutoCondition);
                }
                DecrementTofullAuto.AddCondition(AnimatorConditionMode.Equals, Int32.Parse(fire[1]), fire[0]);
                if (hasFinalShot) DecrementTofullAuto.AddCondition(AnimatorConditionMode.Greater, 1, LayerName + " Ammo Count");
                else DecrementTofullAuto.AddCondition(AnimatorConditionMode.Greater, 0, LayerName + " Ammo Count");
                FireToFullAutoTrans.AddCondition(AnimatorConditionMode.Equals, Int32.Parse(fire[1]), fire[0]);
                FireToFullAutoTrans.AddCondition(AnimatorConditionMode.IfNot, 1, "IsLocal");
                FireToFullAutoTrans.AddCondition(AnimatorConditionMode.IfNot, 1, LayerName + " Ammo Empty");
                FullAutoToFireTrans.AddCondition(AnimatorConditionMode.Equals, Int32.Parse(fire[1]), fire[0]);
                FullAutoToReadyTrans.AddCondition(AnimatorConditionMode.NotEqual, Int32.Parse(fire[1]), fire[0]);

                AssetDatabase.AddObjectToAsset(DecrementTofullAuto, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
                AssetDatabase.AddObjectToAsset(FullAutoToFireTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
                AssetDatabase.AddObjectToAsset(FireToFullAutoTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
                AssetDatabase.AddObjectToAsset(FullAutoToReadyTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));

                DecrementAmmoState.AddTransition(DecrementTofullAuto);
                FullAutoState.AddTransition(FullAutoToFireTrans); 
                FullAutoState.AddTransition(FullAutoToReadyTrans); 
                Firestate.AddTransition(FireToFullAutoTrans);

                //full auto extra parameter driver
                VRCAvatarParameterDriver DecrementAmmoFullAutoCountDriver = (VRCAvatarParameterDriver)FullAutoState.AddStateMachineBehaviour(typeof(VRCAvatarParameterDriver));
                VRCAvatarParameterDriver.Parameter DecrementAmmoFullAuto = new VRCAvatarParameterDriver.Parameter
                {
                    name = LayerName + " Ammo Count",
                    type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Add,
                    value = -1,

                };
                DecrementAmmoFullAutoCountDriver.parameters.Add(DecrementAmmoFullAuto);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=#e0115fff><b>Gunsmith:</b></color> Setup Full Auto for " + LayerName + " with Selectfire = " + selectfire);
            }

            //Setup MagCheck
            if (MagCheck)
            {
                AnimatorState MagOutState = Controller.layers[layerIndex].stateMachine.AddState(MagCheckOutAnim.name, new Vector3(300.0f, 320.0f, 0.0f));
                AnimatorState MagInState = Controller.layers[layerIndex].stateMachine.AddState(MagCheckInAnim.name, new Vector3(300.0f, 260.0f, 0.0f));
                MagOutState.motion = MagCheckOutAnim;
                MagInState.motion = MagCheckInAnim;

                AnimatorStateTransition ReadyToMagOutTrans = new AnimatorStateTransition
                {
                    duration = 0f,
                    exitTime = 0f,
                    hasExitTime = false,
                    destinationState = MagOutState
                };
                AnimatorStateTransition MagOutToMagInTrans = new AnimatorStateTransition
                {
                    duration = 0f,
                    exitTime = 0f,
                    hasExitTime = false,
                    destinationState = MagInState
                };
                AnimatorStateTransition MagInToReadyTrans = new AnimatorStateTransition
                {
                    duration = 0f,
                    exitTime = 1f,
                    hasExitTime = true,
                    destinationState = ReadyToShootState
                };

                string[] Magcheck = GestureLookup(MagCheckGesture);
                ReadyToMagOutTrans.AddCondition(AnimatorConditionMode.Equals, Int32.Parse(Magcheck[1]), Magcheck[0]);
                MagOutToMagInTrans.AddCondition(AnimatorConditionMode.NotEqual, Int32.Parse(Magcheck[1]), Magcheck[0]);

                AssetDatabase.AddObjectToAsset(ReadyToMagOutTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
                AssetDatabase.AddObjectToAsset(MagOutToMagInTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
                AssetDatabase.AddObjectToAsset(MagInToReadyTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));

                ReadyToShootState.AddTransition(ReadyToMagOutTrans);
                MagOutState.AddTransition(MagOutToMagInTrans);
                MagInState.AddTransition(MagInToReadyTrans);

                AssetDatabase.SaveAssets();
            }

            //Setup Chamber Check
            if (ChamberCheck)
            {
                AnimatorState ChamberCheckPullState = Controller.layers[layerIndex].stateMachine.AddState(ChamberCheckPullAnim.name, new Vector3(300.0f, 540.0f, 0.0f));
                AnimatorState ChamberCheckReleaseState = Controller.layers[layerIndex].stateMachine.AddState(ChamberCheckReleaseAnim.name, new Vector3(300.0f, 480.0f, 0.0f));
                ChamberCheckPullState.motion = ChamberCheckPullAnim;
                ChamberCheckReleaseState.motion = ChamberCheckReleaseAnim;

                AnimatorStateTransition ReadyToChamberPullTrans = new AnimatorStateTransition
                {
                    duration = 0f,
                    exitTime = 0f,
                    hasExitTime = false,
                    destinationState = ChamberCheckPullState
                };
                AnimatorStateTransition ChamberPullToReleaseTrans = new AnimatorStateTransition
                {
                    duration = 0f,
                    exitTime = 0f,
                    hasExitTime = true,
                    destinationState = ChamberCheckReleaseState
                };
                AnimatorStateTransition ChamberReleaseToReadyTrans = new AnimatorStateTransition
                {
                    duration = 0f,
                    exitTime = 1f,
                    hasExitTime = true,
                    destinationState = ReadyToShootState
                };

                string[] Chambercheck = GestureLookup(ChamberCheckGesture);
                ReadyToChamberPullTrans.AddCondition(AnimatorConditionMode.Equals, Int32.Parse(Chambercheck[1]), Chambercheck[0]);
                ChamberPullToReleaseTrans.AddCondition(AnimatorConditionMode.NotEqual, Int32.Parse(Chambercheck[1]), Chambercheck[0]);

                AssetDatabase.AddObjectToAsset(ReadyToChamberPullTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
                AssetDatabase.AddObjectToAsset(ChamberPullToReleaseTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
                AssetDatabase.AddObjectToAsset(ChamberReleaseToReadyTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));

                ReadyToShootState.AddTransition(ReadyToChamberPullTrans);
                ChamberCheckPullState.AddTransition(ChamberPullToReleaseTrans);
                ChamberCheckReleaseState.AddTransition(ChamberReleaseToReadyTrans);

                AssetDatabase.SaveAssets();
            }
            //Setup Final Shot
            if (hasFinalShot)
            {
                ForceSyncState.writeDefaultValues = false;
                AnimatorState AwaitFinalShotState = Controller.layers[layerIndex].stateMachine.AddState("Await Final Shot", new Vector3(-640.0f, 410.0f, 0.0f));
                AnimatorState FinalShotState = Controller.layers[layerIndex].stateMachine.AddState(FinalShotAnim.name, new Vector3(-640.0f, 330.0f, 0.0f));
                FinalShotState.motion = FinalShotAnim;
                AnimatorStateTransition DecramentToAwaitFinalTrans = new AnimatorStateTransition()
                {
                    duration = 0f,
                    exitTime = 0f,
                    hasExitTime = false,
                    destinationState = AwaitFinalShotState
                };
                AnimatorStateTransition AwaitFinalToFinalShotTrans = new AnimatorStateTransition()
                {
                    duration = 0f,
                    exitTime = 0f,
                    hasExitTime = false,
                    destinationState = FinalShotState
                };
                AnimatorStateTransition FinalShotToForceEmptyTrans= new AnimatorStateTransition()
                {
                    duration = 0f,
                    exitTime = 1f,
                    hasExitTime = true,
                    destinationState = ForceSyncState
                };
                DecramentToAwaitFinalTrans.AddCondition(AnimatorConditionMode.Less, 2f, LayerName + " Ammo Count");
                DecramentToAwaitFinalTrans.AddCondition(AnimatorConditionMode.NotEqual, Int32.Parse(fire[1]), fire[0]);
                AwaitFinalToFinalShotTrans.AddCondition(AnimatorConditionMode.Equals, Int32.Parse(fire[1]), fire[0]);
                FinalShotToForceEmptyTrans.AddCondition(AnimatorConditionMode.NotEqual, Int32.Parse(fire[1]), fire[0]);

                if (FullAuto)
                {
                    AnimatorStateTransition DecramentToFinalShotTrans = new AnimatorStateTransition()
                    {
                        duration = 0f,
                        exitTime = 0f,
                        hasExitTime = false,
                        destinationState = FinalShotState
                    };
                    DecramentToFinalShotTrans.AddCondition(AnimatorConditionMode.Less, 2f, LayerName + " Ammo Count");
                    DecramentToFinalShotTrans.AddCondition(AnimatorConditionMode.Equals, Int32.Parse(fire[1]), fire[0]);
                    if (selectfire)
                    {
                        DecramentToFinalShotTrans.AddCondition(AnimatorConditionMode.If, 1, LayerName + " SelectFire");
                    }
                    AssetDatabase.AddObjectToAsset(DecramentToFinalShotTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
                    DecrementAmmoState.AddTransition(DecramentToFinalShotTrans);
                    VRCAvatarParameterDriver ForceEmptyOnFinalShot = (VRCAvatarParameterDriver)FinalShotState.AddStateMachineBehaviour(typeof(VRCAvatarParameterDriver));
                    ForceEmptyOnFinalShot.parameters.Add(AmmoEmpty);
                    AssetDatabase.SaveAssets();
                }

                AssetDatabase.AddObjectToAsset(DecramentToAwaitFinalTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
                AssetDatabase.AddObjectToAsset(AwaitFinalToFinalShotTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
                AssetDatabase.AddObjectToAsset(FinalShotToForceEmptyTrans, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));

                DecrementAmmoState.AddTransition(DecramentToAwaitFinalTrans);
                AwaitFinalShotState.AddTransition(AwaitFinalToFinalShotTrans);
                FinalShotState.AddTransition(FinalShotToForceEmptyTrans);
                DecrementAmmoState.RemoveTransition(DecrementToForceSyncTrans);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#e0115fff><b>Gunsmith:</b></color> All tasks complete");
        }

        private void GenerateCustomAnimationSets()
        {
            for (int i = 0; i < CustomAnimations.Count; i++)
            {
                Vector3 position = new Vector3(600.0f, 190.0f, 0.0f);
                List<AnimatorState> animationArray = new List<AnimatorState>();
                List<AnimatorStateTransition> transitions = new List<AnimatorStateTransition>();

                //add entry point
                AnimatorState EntryState;
                if (CustomAnimations[i].EntryPoint != null)
                {
                    EntryState = Controller.layers[layerIndex].stateMachine.AddState(CustomAnimations[i].EntryPoint.name, new Vector3(position.x + (250 * i), position.y, position.z));
                    EntryState.motion = CustomAnimations[i].EntryPoint;
                }
                else
                {
                    EntryState = Controller.layers[layerIndex].stateMachine.AddState("Entry " + i, new Vector3(position.x + (250 * i), position.y, position.z));
                }
                animationArray.Add(EntryState);
                for (int j = 0; j < CustomAnimations[i].Animations.Count; j++)
                {
                    //add other animation states
                    AnimatorState NextState;
                    if (CustomAnimations[i].Animations[j].Animation != null)
                    {
                        NextState = Controller.layers[layerIndex].stateMachine.AddState(CustomAnimations[i].Animations[j].Animation.name, new Vector3(position.x + (250 * i), (position.y) + (60 * (j + 1)), position.z));
                        NextState.motion = CustomAnimations[i].Animations[j].Animation;
                    }
                    else
                    {
                        NextState = Controller.layers[layerIndex].stateMachine.AddState("Animation " + j, new Vector3(position.x + (250 * i), (position.y) + (60 * (j + 1)), position.z));
                    }
                    animationArray.Add(NextState);
                    AnimatorStateTransition nextTransition = new AnimatorStateTransition()
                    {
                        duration = 0f,
                        exitTime = 0f,
                        hasExitTime = true,
                        destinationState = NextState
                    };
                    for (int k = 0; k < CustomAnimations[i].Animations[j].transitions.Count; k++)
                    {
                        if (CustomAnimations[i].Animations[j].transitions[k] == CustomAnimation.TransitionMethod.Gesture)
                        {
                            string[] transitionGestures = GestureLookup(CustomAnimations[i].Animations[j].gesture[k]);
                            nextTransition.AddCondition(CustomAnimations[i].Animations[j].conditionmode[k], Int32.Parse(transitionGestures[1]), transitionGestures[0]);
                        }
                        if (CustomAnimations[i].Animations[j].transitions[k] == CustomAnimation.TransitionMethod.Parameter)
                        {
                            nextTransition.AddCondition(CustomAnimations[i].Animations[j].conditionmode[k], CustomAnimations[i].Animations[j].ParameterValues[k], CustomAnimations[i].Animations[j].ParameterNames[k]);
                        }
                    }
                    AssetDatabase.AddObjectToAsset(nextTransition, AssetDatabase.GetAssetPath(Controller.layers[layerIndex].stateMachine));
                    transitions.Add(nextTransition);
                }
                for (int l = 0; l < transitions.Count; l++)
                {
                    animationArray[l].AddTransition(transitions[l]);
                }
            }
        }
        private int GetLayerNumber(AnimatorController Controller)
        {
            for (int i = 0; i < Controller.layers.Length; i++)
            {
                if (Controller.layers[i].name == LayerName) return i;
            }
            return -1;
        }

        private int StateIndexLookup(AnimationState state)
        {
            ChildAnimatorState[] allstates = Controller.layers[layerIndex].stateMachine.states;
            for (int i = 0; i < allstates.Length; i++)
            {
                if(allstates[i].state.name == state.name)
                {
                    return i;   
                }
            }
            return -1;
        }
        private bool ValidateFields()
        {
            if (Avatar == null)
            {
                Debug.LogError("<color=#e0115fff><b>Gunsmith:</b></color> No Avatar Selected!");
                return false;
            }
            FXAnimator = (RuntimeAnimatorController)Avatar.baseAnimationLayers[4].animatorController;
            if (FXAnimator == null)
            {
                Debug.LogError("<color=#e0115fff><b>Gunsmith:</b></color> This Avatar Does not have an FX Animator Controller");
                return false;
            }
            Expressions = Avatar.expressionParameters;
            if (Expressions == null)
            {
                Debug.LogError("<color=#e0115fff><b>Gunsmith:</b></color> This Avatar Does not have a VRC Expressions Parameters Object");
                return false;
            }
            if (FireAnim == null || FullReloadAnim == null)
            {
                Debug.LogError("<color=#e0115fff><b>Gunsmith:</b></color> All Core animation fields are required");
                return false;
            }
            Debug.Log("<color=#e0115fff><b>Gunsmith:</b></color> All Core Objects are in place.");

            if (LayerName == "")
            {
                Debug.LogError("<color=#e0115fff><b>Gunsmith:</b></color> You must enter a name for the Animation Layer. If a layer matching that name doesnt exist, one will be created");
                return false;
            }
            if (Ammo == 0)
            {
                Debug.LogError("<color=#e0115fff><b>Gunsmith:</b></color> You mush enter a number greater than zero for the number of rounds you would like the weapon to have");
                return false;
            }

            if (ChamberCheck)
            {
                if (ChamberCheckPullAnim == null || ChamberCheckReleaseAnim == null)
                {
                    Debug.LogError("<color=#e0115fff><b>Gunsmith:</b></color> All Chamber Check animation fields are required");
                    return false;
                }
            }

            if (MagCheck)
            {
                if (MagCheckOutAnim == null || MagCheckInAnim == null)
                {
                    Debug.LogError("<color=#e0115fff><b>Gunsmith:</b></color> All Magazine Check animation fields are required");
                    return false;
                }
            }

            Debug.Log("<color=#e0115fff><b>Gunsmith:</b></color> Generating Core State Machine For " + LayerName);
            return true;
        }

        private string[] GestureLookup(Gestures gesture)
        {
            switch (gesture)
            {
                case Gestures.RightNeutral:
                    return new string[2] { "GestureRight", "0" };
                case Gestures.LeftNeutral:
                    return new string[2] { "GestureLeft", "0" };
                case Gestures.RightFist:
                    return new string[2] { "GestureRight", "1" };
                case Gestures.LeftFist:
                    return new string[2] { "GestureLeft", "1" };
                case Gestures.RightHandOpen:
                    return new string[2] { "GestureRight", "2" };
                case Gestures.LeftHandOpen:
                    return new string[2] { "GestureLeft", "2" };
                case Gestures.RightFingerPoint:
                    return new string[2] { "GestureRight", "3" };
                case Gestures.LeftFingerPoint:
                    return new string[2] { "GestureLeft", "3" };
                case Gestures.RightVictory:
                    return new string[2] { "GestureRight", "4" };
                case Gestures.LeftVictory:
                    return new string[2] { "GestureLeft", "4" };
                case Gestures.RightRockNRoll:
                    return new string[2] { "GestureRight", "5" };
                case Gestures.LeftRockNRoll:
                    return new string[2] { "GestureLeft", "5" };
                case Gestures.RightHandGun:
                    return new string[2] { "GestureRight", "6" };
                case Gestures.LeftHandGun:
                    return new string[2] { "GestureLeft", "6" };
                case Gestures.RightThumbsUp:
                    return new string[2] { "GestureRight", "7" };
                case Gestures.LeftThumbsUp:
                    return new string[2] { "GestureLeft", "7" };
                default : return new string[0];
            }
        }
    }
    
    public class CustomAnimations
    {
        public string Name;
        public AnimationClip EntryPoint;
        public List<CustomAnimation> Animations = new List<CustomAnimation>();
    }
    public class CustomAnimation
    {
        public enum TransitionMethod{Gesture, Parameter}
        public AnimationClip Animation;
        public List<TransitionMethod> transitions;
        public List<GunsmithUtility.Gestures> gesture;
        public List<string> ParameterNames;
        public List<float> ParameterValues;
        public List<AnimatorConditionMode> conditionmode;
    }
}