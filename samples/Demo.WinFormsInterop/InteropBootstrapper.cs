﻿using Caliburn.Light;
using System.Windows.Forms.Integration;

namespace Demo.WinFormsInterop
{
    public class InteropBootstrapper : BootstrapperBase
    {
        private readonly ElementHost _elementHost;

        public InteropBootstrapper(ElementHost elementHost)
        {
            _elementHost = elementHost;
        }

        protected override void Configure()
        {
            var viewModel = IoC.GetInstance<MainViewModel>();
            var viewModelLocator = IoC.GetInstance<IViewModelLocator>();
            var viewModelBinder = IoC.GetInstance<IViewModelBinder>();

            var view = viewModelLocator.LocateForModel(viewModel, null);
            viewModelBinder.Bind(viewModel, view, null);

            var activator = viewModel as IActivate;
            if (activator != null)
                activator.Activate();

            _elementHost.Child = view;
        }
    }
}
