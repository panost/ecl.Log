using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ecl.Log.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ecl.Log {
    public static class FactoryExtensions {
        public static ILoggerFactory AddEclFile( this ILoggerFactory factory, string fileName ) {
            factory.AddProvider( new FileLoggerProvider( new FileLoggerSettings {
                FileName = fileName
            } ) );
            return factory;
        }

        public static ILoggingBuilder AddEclFile( this ILoggingBuilder builder, string fileName ) {
            //builder.Services.AddSingleton<ILoggerProvider, ConsoleLoggerProvider>();
            //builder.AddProvider( new FileLoggerProvider( new FileLoggerOptions( fileName ) ) );
            //builder.AddFilter<FileLoggerProvider>( null, LogLevel.Trace );
            builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
            //builder.Services.AddSingleton<IConfigureOptions<FileLoggerOptions>, FileLoggerOptionsSetup>();
            //builder.Services.AddSingleton<IOptionsChangeTokenSource<FileLoggerOptions>, 
            //    LoggerProviderOptionsChangeTokenSource<FileLoggerOptions, FileLoggerProvider>>();

            return builder;
        }

        public static ILoggerFactory AddEclFile( this ILoggerFactory factory, FileLoggerSettings settings ) {
            factory.AddProvider( new FileLoggerProvider( settings ) );
            return factory;
        }

        public static ILoggerFactory AddEclFile( this ILoggerFactory factory, IConfiguration configuration ) {
            var settings = new FileLoggerSettings( configuration );

            return factory.AddEclFile( settings );
        }

    }
}
