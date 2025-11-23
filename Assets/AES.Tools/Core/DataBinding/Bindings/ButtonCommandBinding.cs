using System;
using System.Globalization;
using AES.Tools.Commands;
using UnityEngine;
using UnityEngine.UI;


namespace AES.Tools.Bindings
{
    public enum ParameterType
    {
        None, String, Int,
        Float, Bool
    }

    [RequireComponent(typeof(Button))]
    public class ButtonCommandBinding : ContextBindingBase
    {
        [Header("Target")]
        [SerializeField] private Button button;

        [Header("Parameter (optional)")]
        [SerializeField] private bool useParameter;
        [SerializeField, ShowIf(nameof(useParameter))] private ParameterType parameterType = ParameterType.None;
        [SerializeField, ShowIf(nameof(useParameter))] private string stringParameter;

        [Header("Behaviour")]
        [SerializeField] private bool updateInteractableOnEnable = true;
        [SerializeField] private bool disableWhileRunning = true;

        private ICommand _command;

#if UNITY_EDITOR
        private void Reset()
        {
            if (button == null)
                button = GetComponent<Button>();
        }
#endif

        protected override void Subscribe()
        {
            if (button == null)
            {
                Debug.LogError("ButtonCommandBinding: Button 이 설정되지 않았습니다.", this);
                return;
            }

            if (Context == null || Path == null || Context.ViewModel == null)
                return;

            object value;

            try { value = Path.GetValue(Context.ViewModel); }
            catch (Exception e)
            {
                Debug.LogError($"ButtonCommandBinding: Path '{memberPath}' 조회 실패: {e.Message}", this);
                return;
            }

            if (value is ICommand cmd) { BindCommand(cmd); }
            else
            {
                Debug.LogError(
                    $"ButtonCommandBinding: Path '{memberPath}' 는 ICommand 타입이 아닙니다 ({value?.GetType().Name}).",
                    this);

                return;
            }

            button.onClick.AddListener(OnClick);

            if (updateInteractableOnEnable)
                UpdateInteractable();
        }

        protected override void Unsubscribe()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClick);

            _command = null;
        }

        private void BindCommand(ICommand cmd)
        {
            _command = cmd;

            UpdateInteractable();
        }

        private void UpdateInteractable()
        {
            if (button == null || _command == null)
                return;

            var param = GetParameterObject();
            var canExecute = _command.CanExecute(param);

            if (disableWhileRunning)
                button.interactable = canExecute && !_isRunning;
            else
                button.interactable = canExecute;
        }

        private bool _isRunning;

        private async void OnClick()
        {
            if (_isRunning)
                return; // 연속 탭 방지

            var param = GetParameterObject();
            if (!_command.CanExecute(param))
                return;

            _isRunning = true;
            UpdateInteractable();

            try
            {
                if (_command is IAsyncCommand asyncCmd)
                    await asyncCmd.ExecuteAsync(param);
                else
                    _command.Execute(param);
            }
            finally
            {
                _isRunning = false;
                UpdateInteractable();
            }
        }

        private object GetParameterObject()
        {
            if (!useParameter)
                return null;

            if (string.IsNullOrEmpty(stringParameter))
                return null;

            try
            {
                switch (parameterType)
                {
                    case ParameterType.String:
                        return stringParameter;

                    case ParameterType.Int:
                        if (int.TryParse(stringParameter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                            return i;

                        Debug.LogWarning($"ButtonCommandBinding: int 변환 실패 '{stringParameter}'", this);
                        return null;

                    case ParameterType.Float:
                        if (float.TryParse(stringParameter, NumberStyles.Float | NumberStyles.AllowThousands,
                            CultureInfo.InvariantCulture, out var f))
                            return f;

                        Debug.LogWarning($"ButtonCommandBinding: float 변환 실패 '{stringParameter}'", this);
                        return null;

                    case ParameterType.Bool:
                        // true/false, 0/1 둘 다 지원
                        if (bool.TryParse(stringParameter, out var b))
                            return b;

                        if (stringParameter == "0") return false;
                        if (stringParameter == "1") return true;

                        Debug.LogWarning($"ButtonCommandBinding: bool 변환 실패 '{stringParameter}'", this);
                        return null;

                    case ParameterType.None:
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ButtonCommandBinding: 파라미터 변환 실패 '{stringParameter}': {e.Message}", this);
                return null;
            }
        }

        private void Update()
        {
            if (button == null || _command == null || !updateInteractableOnEnable) return;
            
            UpdateInteractable();
        }
    }
}