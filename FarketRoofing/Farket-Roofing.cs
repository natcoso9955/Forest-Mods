// RoofArchitect
using Bolt;
using System.Collections;
using System.Collections.Generic;
using TheForest.Buildings.Creation;
using TheForest.Buildings.Interfaces;
using TheForest.Buildings.World;
using TheForest.Items;
using TheForest.Utils;
using ModAPI.Attributes;
using UniLinq;
using UnityEngine;

namespace FarketRoofing
{
    public class Farket_Roofing : RoofArchitect
    {

        protected override void Update()
        {
            bool edgeCrossing = false;
            bool flag = false;
            bool flag2 = _multiPointsPositions.Count >= 3; //&& _roofHeight < _minHeight;
                if (LocalPlayer.Create.BuildingPlacer.Clear != flag2)
                {
                    LocalPlayer.Create.BuildingPlacer.Clear = flag2;
                }
                if (_lockMode == LockModes.Shape && TheForest.Utils.Input.GetButtonDown("Craft"))
                {
                    _autofillmode = !_autofillmode;
                    PlayerPrefs.SetInt("ExFloorsAutofill", PlayerPreferences.ExFloorsAutofill ? 1 : 0);
                    PlayerPrefs.Save();
                    UpdateAutoFill(doCleanUp: true);
                    TheForest.Utils.Scene.HudGui.MultipointShapeGizmo.Shutdown();
                }
                if (TheForest.Utils.Input.GetButtonDown("AltFire") && _multiPointsPositions.Count > 0)
                {
                    if (_multiPointsPositions.Count == 1)
                    {
                        if ((bool)_roofRoot)
                        {
                            Object.Destroy(_roofRoot.gameObject);
                            _roofRoot = null;
                        }
                        _newPool.Clear();
                        _logPool.Clear();
                        TheForest.Utils.Scene.HudGui.MultipointShapeGizmo.Shutdown();
                    }
                    _multiPointsPositions.RemoveAt(_multiPointsPositions.Count - 1);
                    if (_lockMode == LockModes.Height)
                    {
                        _lockMode = LockModes.Shape;
                        LocalPlayer.Sfx.PlayWhoosh();
                    }
                    _roofHeight = 0f;
                }
                if (!_autofillmode)
                {
                    if (_lockMode == LockModes.Shape)
                    {
                        flag = UpdateShape(out edgeCrossing);
                    }
                    else
                    {
                        UpdateHeight();
                    }
                }
                else if (_lockMode == LockModes.Shape)
                {
                    if (!edgeCrossing && _multiPointsPositions.Count >= 3 && TheForest.Utils.Input.GetButtonDown("Build"))
                    {
                        if (CurrentSupport != null)
                        {
                            LocalPlayer.Create.BuildingPlacer.ForcedParent = (CurrentSupport as MonoBehaviour).gameObject;
                        }
                        _lockMode = LockModes.Height;
                        LocalPlayer.Sfx.PlayWhoosh();
                    }
                    _caster.CastForAnchors<PrefabIdentifier>(CheckTargetingSupport);
                }
                else
                {
                    UpdateHeight();
                }
                if (_multiPointsPositions.Count > 0)
                {
                    if (!_autofillmode && _lockMode == LockModes.Shape)
                    {
                        TheForest.Utils.Scene.HudGui.MultipointShapeGizmo.Show(this, new Vector3(0f, 0.15f, 0f));
                    }
                    if (_multiPointsPositions.Count == 1)
                    {
                        Vector3 vector = GetCurrentEdgePoint();
                        if (Vector3.Distance(vector, _multiPointsPositions[0]) < _logWidth / 2f)
                        {
                            vector = _multiPointsPositions[0] + (vector - _multiPointsPositions[0]).normalized * 0.5f;
                        }
                        _multiPointsPositions.Add(vector);
                        Vector3 b = (_multiPointsPositions[1] - _multiPointsPositions[0]).RotateY(-90f).normalized * 0.5f;
                        _multiPointsPositions.Add(_multiPointsPositions[1] + b);
                        _multiPointsPositions.Add(_multiPointsPositions[0] + b);
                        _multiPointsPositions.Add(_multiPointsPositions[0]);
                        RefreshCurrentRoof();
                        _multiPointsPositions.RemoveRange(1, 4);
                    }
                    else
                    {
                        bool flag3 = false;
                        bool flag4 = _lockMode == LockModes.Height || (_multiPointsPositions.Count > 2 && Vector3.Distance(_multiPointsPositions[0], _multiPointsPositions[_multiPointsPositions.Count - 1]) < _closureSnappingDistance);
                        if (!flag4)
                        {
                            Vector3 currentEdgePoint = GetCurrentEdgePoint();
                            if (Vector3.Distance(currentEdgePoint, _multiPointsPositions[0]) > _closureSnappingDistance)
                            {
                                flag3 = true;
                                _multiPointsPositions.Add(currentEdgePoint);
                            }
                            if (_multiPointsPositions.Count > 2 || !flag3)
                            {
                                _multiPointsPositions.Add(_multiPointsPositions[0]);
                            }
                        }
                        RefreshCurrentRoof();
                        if (!flag4)
                        {
                            if (_multiPointsPositions.Count > 2 || !flag3)
                            {
                                _multiPointsPositions.RemoveAt(_multiPointsPositions.Count - 1);
                            }
                            if (flag3)
                            {
                                _multiPointsPositions.RemoveAt(_multiPointsPositions.Count - 1);
                            }
                        }
                    }
                }
                else if (CurrentSupport != null)
                {
                    TheForest.Utils.Scene.HudGui.MultipointShapeGizmo.Show(this, new Vector3(0f, 0.15f, 0f));
                    Vector3 currentEdgePoint2 = GetCurrentEdgePoint();
                    _multiPointsPositions.Add(currentEdgePoint2);
                    Vector3 b2 = LocalPlayer.Transform.right * _logWidth * 1f;
                    _multiPointsPositions.Add(_multiPointsPositions[0] + b2);
                    Vector3 b3 = (_multiPointsPositions[1] - _multiPointsPositions[0]).RotateY(-90f).normalized * 0.5f;
                    _multiPointsPositions.Add(_multiPointsPositions[1] + b3);
                    _multiPointsPositions.Add(_multiPointsPositions[0] + b3);
                    _multiPointsPositions.Add(_multiPointsPositions[0]);
                    RefreshCurrentRoof();
                    _multiPointsPositions.Clear();
                }
                else
                {
                    TheForest.Utils.Scene.HudGui.MultipointShapeGizmo.Shutdown();
                }
                bool flag6 = Create.CanLock = ((_lockMode == LockModes.Height) ? flag2 : (!edgeCrossing && (flag || flag2 || _multiPointsPositions.Count > 0)));
                Renderer component = GetComponent<Renderer>();
                if ((bool)component)
                {
                    component.sharedMaterial = Create.CurrentGhostMat;
                    component.enabled = (CurrentSupport == null && _multiPointsPositions.Count == 0);
                }
                bool flag7 = _lockMode == LockModes.Height;
                bool flag8 = !_autofillmode && _multiPointsPositions.Count == 0 && CurrentSupport != null;
                bool canToggleAutofill = _lockMode == LockModes.Shape && (flag || flag2 || _multiPointsPositions.Count > 0 || CurrentSupport != null);
                bool canLock = !flag8 && flag && _lockMode == LockModes.Shape;
                bool canUnlock = !_autofillmode && !flag7 && _multiPointsPositions.Count > 0 && _lockMode == LockModes.Shape;
                bool showAutofillPlace = _autofillmode && !flag7 && !edgeCrossing && _multiPointsPositions.Count >= 3;
                bool showManualPlace = !_autofillmode && !flag7 && !edgeCrossing && _multiPointsPositions.Count >= ((!flag) ? 3 : 2);
                TheForest.Utils.Scene.HudGui.RoofConstructionIcons.Show(flag8, canToggleAutofill, showAutofillPlace, showManualPlace, canLock, canUnlock, flag7);
            }

        

    }

     }