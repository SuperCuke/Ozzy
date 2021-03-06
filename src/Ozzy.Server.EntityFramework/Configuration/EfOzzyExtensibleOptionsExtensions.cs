﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ozzy.Server.EntityFramework
{
    public static class EfOzzyExtensibleOptionsExtensions
    {
        public static DbContextOptions GetDbContextOptions(this IExtensibleOptions options)
        {
            var extension = options.FindExtension<CoreOptionsExtension>();
            return extension.ServiceProvider.GetService<DbContextOptions>();
        }        
    }
}
