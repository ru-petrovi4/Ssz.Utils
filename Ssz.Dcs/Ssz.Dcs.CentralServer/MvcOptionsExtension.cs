using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Linq;

namespace Ssz.Dcs.CentralServer
{
    public static class MvcOptionsExtension
    {
        public static void UseCentralRoutePrefix(this MvcOptions options, IRouteTemplateProvider routeAttribute)
        {
            options.Conventions.Insert(0, new RouteConvention(routeAttribute));
        }

        public class RouteConvention : IApplicationModelConvention
        {
            private readonly AttributeRouteModel _centralPrefix;

            public RouteConvention(IRouteTemplateProvider routeTemplateProvider)
            {
                _centralPrefix = new AttributeRouteModel(routeTemplateProvider);
            }

            public void Apply(ApplicationModel application)
            {
                foreach (var controller in application.Controllers)
                {
                    foreach (var selectorModel in controller.Selectors)
                    {
                        if (selectorModel.AttributeRouteModel is not null)
                            selectorModel.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(_centralPrefix,
                                selectorModel.AttributeRouteModel);
                    }                    
                }
            }
        }
    }
}
