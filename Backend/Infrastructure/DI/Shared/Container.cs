using System.Reflection;
using WebApp.Pricing.Calculator.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DI.Shared;

public static class Container
{
        public static IServiceCollection AutoInjectAll(this IServiceCollection services)
        {
            // Pega todos os assemblies carregados
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            // Interfaces de marcação que devemos ignorar no registro
            var markerInterfaces = new[] 
            { 
                typeof(IScopedService), 
                typeof(ITransientService), 
                typeof(ISingletonService) 
            };

            foreach(Assembly ass in assemblies)
            {
                var types = ass.GetTypes().Where(type =>
                    !type.IsAbstract &&
                    !type.IsInterface &&
                    type.GetInterfaces().Any(i => markerInterfaces.Contains(i)));

                foreach(var type in types)
                {
                    // Descobre o ciclo de vida com base na interface implementada
                    var isScoped = typeof(IScopedService).IsAssignableFrom(type);
                    var isTransient = typeof(ITransientService).IsAssignableFrom(type);
                    var isSingleton = typeof(ISingletonService).IsAssignableFrom(type);

                    // Pega as interfaces reais do serviço (ignorando as de marcação)
                    var abstractions = type.GetInterfaces()
                        .Where(i => !markerInterfaces.Contains(i));

                    // 1. Registra o tipo concreto primeiro, garantindo que seja registrado apenas uma vez
                    if (!services.Any(service => service.ServiceType == type))
                    {
                        if (isScoped) services.AddScoped(type);
                        else if (isTransient) services.AddTransient(type);
                        else if (isSingleton) services.AddSingleton(type);
                    }

                    // 2. Registra cada interface apontando para o tipo concreto
                    foreach (var abstraction in abstractions)
                    {
                        if (isScoped || isSingleton)
                        {
                            // Para Scoped e Singleton, resolvemos do provider para manter a mesma instância
                            if (isScoped)
                                services.AddScoped(abstraction, resolver => resolver.GetRequiredService(type));
                            else
                                services.AddSingleton(abstraction, resolver => resolver.GetRequiredService(type));
                        }
                        else if (isTransient)
                        {
                            // Para Transient, sempre gera uma nova instância
                            services.AddTransient(abstraction, type);
                        }
                    }
                }
            }
            return services;
        }
}