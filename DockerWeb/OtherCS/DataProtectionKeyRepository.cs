using DockerWeb.DB;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DockerWeb.OtherCS
{
    public class DataProtectionKeyRepository : IXmlRepository
    {
        private readonly CoreDataContext _db;

        public DataProtectionKeyRepository(CoreDataContext context)
        {
            _db = context;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            return new ReadOnlyCollection<XElement>(_db.DataProtectionKeys.Select(k => XElement.Parse(k.XmlData)).ToList());
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            var entity = _db.DataProtectionKeys.SingleOrDefault(k => k.FriendlyName == friendlyName);
            if (null != entity)
            {
                entity.XmlData = element.ToString();
                _db.DataProtectionKeys.Update(entity);
            }
            else
            {
                _db.DataProtectionKeys.Add(new DataProtectionKeys
                {
                    FriendlyName = friendlyName,
                    XmlData = element.ToString()
                });
            }

            _db.SaveChanges();
        }
    }
}
