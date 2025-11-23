using System;
using UnityEngine;


namespace AES.Tools
{
       // ContextBindingBase: DataContext + Path 기반 공통 베이스
    public abstract class ContextBindingBase : BindingBehaviour
    {
        [SerializeField]
        [Tooltip("ViewModel의 멤버 경로")]
        protected string memberPath;

        DataContextBase _context;
        MemberPath _path;

        protected DataContextBase Context
        {
            get
            {
                if (_context == null)
                    _context = GetComponentInParent<DataContextBase>();
                return _context;
            }
        }

        protected MemberPath Path
        {
            get
            {
                if (_path == null && Context != null && Context.ViewModel != null && !string.IsNullOrEmpty(memberPath))
                {
                    var type = Context.ViewModel.GetType();
                    try
                    {
                        _path = MemberPathCache.Get(type, memberPath);
                    }
                    catch (Exception ex)
                    {
                        LogBindingException($"memberPath '{memberPath}' 해석 실패", ex);
                    }
                }

                return _path;
            }
        }

        /// <summary>
        /// Context + Path 해석 공통 진입점.
        /// 실패 시 false 반환 + 내부에서 로그 출력.
        /// </summary>
        protected bool TryResolvePath(out object viewModel, out MemberPath path)
        {
            viewModel = null;
            path = null;

            if (Context == null)
            {
                LogBindingError("상위 DataContextBase 를 찾지 못했습니다.");
                return false;
            }

            viewModel = Context.ViewModel;
            if (viewModel == null)
            {
                LogBindingError("Context.ViewModel 이 null 입니다.");
                return false;
            }

            if (string.IsNullOrEmpty(memberPath))
            {
                LogBindingError("memberPath 가 비어 있습니다.");
                return false;
            }

            if (Path == null)
            {
                // Path 프로퍼티 안에서 이미 에러 로그를 찍기 때문에 여기서는 false만 리턴
                return false;
            }

            path = Path;
            return true;
        }
        
        protected ObservableProperty<T> ResolveObservableProperty<T>()
        {
            if (!TryResolvePath(out var vm, out var path))
                return null;

            var value = path.GetValue(vm);
            if (value is ObservableProperty<T> prop)
                return prop;
            
            Debug.LogError($"멤버 '{memberPath}' 는 ObservableProperty<{typeof(T).Name}> 가 아닙니다.", this);
            return null;
        }

        /// <summary>
        /// IObservableProperty (boxing 허용) 해석.
        /// </summary>
        protected IObservableProperty ResolveObservablePropertyBoxed()
        {
            if (!TryResolvePath(out var vm, out var path))
                return null;

            var value = path.GetValue(vm);
            if (value is IObservableProperty prop)
                return prop;

            LogBindingError($"멤버 '{memberPath}' 는 IObservableProperty 가 아닙니다. 실제 타입: {value?.GetType().Name ?? "null"}");
            return null;
        }

        /// <summary>
        /// IObservableList 해석.
        /// </summary>
        protected IObservableList ResolveObservableList()
        {
            if (!TryResolvePath(out var vm, out var path))
                return null;

            var value = path.GetValue(vm);
            if (value is IObservableList list)
                return list;

            LogBindingError($"멤버 '{memberPath}' 는 IObservableList 가 아닙니다. 실제 타입: {value?.GetType().Name ?? "null"}");
            return null;
        }
    }

}