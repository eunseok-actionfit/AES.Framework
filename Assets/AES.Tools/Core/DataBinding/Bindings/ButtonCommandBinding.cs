// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// using AES.Tools.Converters;
//
//
//
// namespace AES.Tools.Bindings
// {
//     public enum ParameterType
//     {
//         None, String, Int,
//         Float, Bool
//     }
//
//     /// <summary>
//     /// DataContext + memberPath 기반 ICommand 바인딩 + 파라미터 변환 + CanExecute 자동 반영
//     /// </summary>
//     [RequireComponent(typeof(Button))]
//     public class ButtonCommandBinding : ContextBindingBase
//     {
//         [Header("Target")]
//         [SerializeField] private Button button;
//
//         [Header("Parameter (optional)")]
//         [SerializeField] private bool useParameter;
//         [SerializeField] private ParameterType parameterType = ParameterType.None;
//         [SerializeField] private string stringParameter;
//
//         [Header("Behaviour")]
//         [SerializeField] private bool updateInteractableOnEnable = true;
//
//         private ICommand _command;
//
//         // 기본 컨버터 테이블
//         private static readonly Dictionary<ParameterType, IParameterConverter> BuiltInConverters =
//             new()
//             {
//                 { ParameterType.String, new StringConverter() },
//                 { ParameterType.Int, new IntConverter() },
//                 { ParameterType.Float, new FloatConverter() },
//                 { ParameterType.Bool, new BoolConverter() }
//             };
//
//         
//         private void Reset() => button = GetComponent<Button>();
//         protected override void Subscribe()
//         {
//             if (button == null)
//             {
//                 Debug.LogError("ButtonCommandBinding: Button 이 설정되지 않았습니다.", this);
//                 return;
//             }
//
//             if (Context == null || Path == null || Context.ViewModel == null)
//                 return;
//
//             object value;
//
//             try { value = Path.GetValue(Context.ViewModel); }
//             catch (Exception e)
//             {
//                 Debug.LogError($"ButtonCommandBinding: Path '{memberPath}' 조회 실패: {e.Message}", this);
//                 return;
//             }
//
//             if (value is ICommand cmd) { BindCommand(cmd); }
//             else
//             {
//                 Debug.LogError(
//                     $"ButtonCommandBinding: Path '{memberPath}' 는 ICommand 타입이 아닙니다 ({value?.GetType().Name}).",
//                     this);
//
//                 return;
//             }
//
//             button.onClick.AddListener(OnClick);
//
//             if (updateInteractableOnEnable)
//                 UpdateInteractable();
//         }
//
//         protected override void Unsubscribe()
//         {
//             if (button != null)
//                 button.onClick.RemoveListener(OnClick);
//
//             _command.CanExecuteChanged -= OnCanExecuteChanged;
//             _command = null;
//         }
//
//         private void BindCommand(ICommand cmd)
//         {
//             if (_command != null)
//                 _command.CanExecuteChanged -= OnCanExecuteChanged;
//
//             _command = cmd;
//
//             if (_command != null)
//                 _command.CanExecuteChanged += OnCanExecuteChanged;
//
//             UpdateInteractable();
//         }
//         
//
//         private void OnCanExecuteChanged()
//         {
//             UpdateInteractable();
//         }
//
//         private void UpdateInteractable()
//         {
//             if (button == null || _command == null) return;
//             button.interactable = _command.CanExecute(GetParameterObject());
//         }
//
//         private void OnClick()
//         {
//             if (_command == null) return;
//
//             var param = GetParameterObject();
//             if (_command.CanExecute(param))
//                 _command.Execute(param);
//         }
//
//         private object GetParameterObject()
//         {
//             if (!useParameter || parameterType == ParameterType.None)
//                 return null;
//
//             // 내장 컨버터 사용
//             if (BuiltInConverters.TryGetValue(parameterType, out var conv))
//             {
//                 if (conv.TryConvert(stringParameter, out var result))
//                     return result;
//
//                 Debug.LogWarning(
//                     $"ButtonCommandBinding: 파라미터 변환 실패 '{stringParameter}' → {parameterType}",
//                     this);
//             }
//
//             return null;
//         }
//     }
// }