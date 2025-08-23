using Microsoft.Extensions.DependencyInjection;
using NVRNotifier.Core.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NVRNotifier.Core
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCore(this IServiceCollection serviceColleciton)
        {
            serviceColleciton.AddSingleton<IAppSettings, AppSettings>();
            return serviceColleciton;
        }
    }
}
