﻿using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(BasicWebsite.Startup))]
namespace BasicWebsite
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {

        }
    }
}
