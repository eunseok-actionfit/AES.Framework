// using System;
//
// namespace AES.Tools
// {
//     public abstract class MonoContext<TViewModel> : MonoContextHolder
//         where TViewModel : class
//     {
//         public TViewModel ViewModelTyped => (TViewModel)ViewModel;
//
//         public override Type ViewModelType => typeof(TViewModel);
//
//         protected sealed override object CreateViewModel()
//         {
//             return CreateViewModelTyped();
//         }
//
//         /// <summary>
//         /// AutoCreate 모드에서 사용할 ViewModel 생성 로직.
//         /// </summary>
//         protected virtual TViewModel CreateViewModelTyped() => null;
//
//         public void SetViewModel(TViewModel vm, bool recreateContext = true)
//         {
//             base.SetViewModel(vm, recreateContext);
//         }
//     }
// }