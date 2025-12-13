using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;


namespace AES.Tools
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
        [SerializeField] Button button;

        [Header("Parameter (optional)")]
        [SerializeField] bool useParameter;
        [SerializeField, ShowIf(nameof(useParameter))] ParameterType parameterType = ParameterType.None;
        [SerializeField, ShowIf(nameof(useParameter))] string stringParameter;

        [Header("Behaviour")]
        [SerializeField] bool updateInteractableOnEnable = true;
        [SerializeField] bool disableWhileRunning = true;

        ICommand _command;

        bool _isRunning;

#if UNITY_EDITOR
        void Reset()
        {
            if (button == null)
                button = GetComponent<Button>();
        }
#endif

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            if (button == null)
            {
                Debug.LogError("ButtonCommandBinding: Button 이 설정되지 않았습니다.", this);
                return;
            }

            object value;
            try
            {
                value = context.GetValue(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"ButtonCommandBinding: Path '{path}' 조회 실패: {e.Message}", this);
                return;
            }

            if (value is ICommand cmd)
            {
                BindCommand(cmd);

#if UNITY_EDITOR
                // 커맨드는 처음 한번만 디버그에 기록
                Debug_OnValueUpdated(cmd, path);
#endif
            }
            else
            {
                Debug.LogError(
                    $"ButtonCommandBinding: Path '{path}' 는 ICommand 타입이 아닙니다 ({value?.GetType().Name}).",
                    this);
                return;
            }

            button.onClick.AddListener(OnClick);

            if (updateInteractableOnEnable)
                UpdateInteractable();
        }


        protected override void OnContextUnavailable()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClick);

            _command = null;
        }

        void BindCommand(ICommand cmd)
        {
            _command = cmd;
            UpdateInteractable();
        }

        void UpdateInteractable()
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

        async void OnClick()
        {
            if (_isRunning || _command == null)
                return;

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

        object GetParameterObject()
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
                        if (float.TryParse(stringParameter,
                                NumberStyles.Float | NumberStyles.AllowThousands,
                                CultureInfo.InvariantCulture, out var f))
                            return f;
                        Debug.LogWarning($"ButtonCommandBinding: float 변환 실패 '{stringParameter}'", this);
                        return null;

                    case ParameterType.Bool:
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

        void Update()
        {
            if (button == null || _command == null || !updateInteractableOnEnable)
                return;

            UpdateInteractable();
        }
    }
}
