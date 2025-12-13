using System;
using System.Globalization;
using TMPro;
using UnityEngine;


namespace AES.Tools
{
    [RequireComponent(typeof(TMP_InputField))]
    public class InputFieldCommandBinding : ContextBindingBase
    {
        [Header("Trigger")]
        [SerializeField] bool triggerOnEndEdit = true;
        [SerializeField] bool triggerOnSubmit = false;

        [Header("Parameter")]
        [SerializeField] bool useTextAsParameter = true;
        [SerializeField, ShowIf(nameof(useTextAsParameter))] ParameterType parameterType = ParameterType.String;
        [SerializeField] bool useInvariantCulture = true;

        TMP_InputField input;
        ICommand _command;
        IBindingContext _ctx;

        bool _isRunning;

        void OnValidate()
        {
            input ??= GetComponent<TMP_InputField>();
        }

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            if (input == null)
                input = GetComponent<TMP_InputField>();

            if (input == null)
            {
                LogBindingError("InputFieldCommandBinding: TMP_InputField 가 설정되지 않았습니다.");
                return;
            }

            _ctx = context;

            // Path 에서 ICommand 가져오기
            object value;
            try
            {
                value = context.GetValue(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"InputFieldCommandBinding: Path '{path}' 조회 실패: {e.Message}", this);
                return;
            }

            if (value is ICommand cmd)
            {
                _command = cmd;
            }
            else
            {
                Debug.LogError(
                    $"InputFieldCommandBinding: Path '{path}' 는 ICommand 타입이 아닙니다 ({value?.GetType().Name}).",
                    this);
                return;
            }

            // 트리거 등록
            if (triggerOnEndEdit)
                input.onEndEdit.AddListener(OnSubmit);

            if (triggerOnSubmit)
                input.onSubmit.AddListener(OnSubmit);
        }

        protected override void OnContextUnavailable()
        {
            if (input != null)
            {
                input.onEndEdit.RemoveListener(OnSubmit);
                input.onSubmit.RemoveListener(OnSubmit);
            }

            _ctx = null;
            _command = null;
        }

        void OnSubmit(string _)
        {
            if (_isRunning || _command == null)
                return;

            var param = useTextAsParameter
                ? GetParameterObject(input != null ? input.text : null)
                : null;

            if (!_command.CanExecute(param))
                return;

            _isRunning = true;

            ExecuteCommandAsync(param);
        }

        async void ExecuteCommandAsync(object param)
        {
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
            }
        }

        object GetParameterObject(string text)
        {
            if (!useTextAsParameter)
                return null;

            if (string.IsNullOrEmpty(text))
                return null;

            var culture = useInvariantCulture
                ? CultureInfo.InvariantCulture
                : CultureInfo.CurrentCulture;

            try
            {
                switch (parameterType)
                {
                    case ParameterType.String:
                        return text;

                    case ParameterType.Int:
                        if (int.TryParse(text, NumberStyles.Integer, culture, out var i))
                            return i;
                        Debug.LogWarning($"InputFieldCommandBinding: int 변환 실패 '{text}'", this);
                        return null;

                    case ParameterType.Float:
                        if (float.TryParse(text,
                                NumberStyles.Float | NumberStyles.AllowThousands,
                                culture, out var f))
                            return f;
                        Debug.LogWarning($"InputFieldCommandBinding: float 변환 실패 '{text}'", this);
                        return null;

                    case ParameterType.Bool:
                        if (bool.TryParse(text, out var b))
                            return b;
                        if (text == "0") return false;
                        if (text == "1") return true;
                        Debug.LogWarning($"InputFieldCommandBinding: bool 변환 실패 '{text}'", this);
                        return null;

                    case ParameterType.None:
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"InputFieldCommandBinding: 파라미터 변환 실패 '{text}': {e.Message}", this);
                return null;
            }
        }
    }
}
