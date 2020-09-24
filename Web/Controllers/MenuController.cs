﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Web.DTO;
using Web.Models.Menu;

namespace Web.Controllers
{
    public class MenuController : Controller
    {
        private readonly IMenuService<MenuItem> menuService;

        public MenuController(IMenuService<MenuItem> menuService)
        {
            this.menuService = menuService;
        }

        [HttpGet("/")]
        public IActionResult Index()
        {
            return Redirect("/menu");
        }

        //Should precede SingleItem method
        [HttpGet("/menu/new")]
        public IActionResult NewItem()
        {
            return View();
        }


        [HttpGet("/menu")]
        public IActionResult Menu(int page = 1, string orderColumn = "", string orderType = null, MenuItem searchFields = null)
        {
            List<MenuItem> items;
            int itemCount = 20;
            int downItem = itemCount * (page - 1);
            var totalPageNum = (menuService.Count - 1) / itemCount + 1;

            if (searchFields.Title == default && searchFields.Description == default && 
                searchFields.Ingredients == default && searchFields.CreationDate == default && searchFields.Id == default
                && searchFields.Price == default && searchFields.Grams == default && searchFields.Calories == default && searchFields.CookingTime == default)
            {
                searchFields = null;
            }
            
            if (orderColumn != "" && orderType != null && orderColumn != "Ingredients")
            {
                orderColumn = Regex.Replace(orderColumn, " ", String.Empty);
                items = menuService.ListAllItems(downItem, itemCount, orderColumn, orderType, searchFields);
            }
            else
            {
                items = menuService.ListAllItems(downItem, itemCount, searchFields);
            }

            return View( new MenuModel(
                menuItems: items, 
                totalPageNum: totalPageNum, 
                pageNum: page, 
                orderParams: orderColumn.ToLower() + "-" + orderType, 
                searchFields: searchFields
                ));
        }

        [HttpPost("/menu")]
        public IActionResult CreateItem([Bind("Title,Ingredients,Description,Price,Grams,Calories,CookingTime")] DTO.MenuItemDTO item)
        {
            try
            {
                menuService.AddNewItem(Mapper.Map(item));

            }
            catch (DbUpdateException dbexc)
            {
                var innerexc = dbexc.InnerException;
                string columnName = String.Empty;
                if (innerexc is SqlException)
                {
                    var columnNameMatch = Regex.Match(innerexc.Message, @"\(\w*|\d\)");
                    if (columnNameMatch.Success)
                    {
                        var reg = new Regex(@"\(|\)");
                        columnName = reg.Replace(columnNameMatch.Value, String.Empty);  
                    }
                    ModelState.AddModelError("Title#" ,$"The given title '{columnName}' have already been created ");
                    return View("NewItem", item);
                }
                throw;
            }

            return Redirect("/menu");
        }

        [HttpGet("/menu/{id}")]
        public IActionResult SingleItem(int id)
        {
            var item = menuService.GetItem(id);

            var sendItem = Mapper.Map(item);

            return View(new SingleItemModel(sendItem));
        }

        [HttpPost("/menu/{id}")]
        public IActionResult UpdateItem(DTO.MenuItemDTO item)
        {
            if (!ModelState.IsValid)
            {
                return View("SingleItem", new SingleItemModel(item, true));
            }
            var sendItem = Mapper.Map(item);
            try
            {
                sendItem = menuService.ChangeItem(sendItem);
            }
            catch (DbUpdateException dbexc)
            {
                var innerexc = dbexc.InnerException;
                string columnName = String.Empty;
                if (innerexc is SqlException)
                {
                    var columnNameMatch = Regex.Match(innerexc.Message, @"\(\w*|\d\)");
                    if (columnNameMatch.Success)
                    {
                        var reg = new Regex(@"\(|\)");
                        columnName = reg.Replace(columnNameMatch.Value, String.Empty);
                    }
                    ModelState.AddModelError("MenuItem.Title#", $"Given title '{columnName}' have already been created ");
                    return View("SingleItem", new SingleItemModel(item, true));
                }
                throw;
            }


            return RedirectToAction("SingleItem", new { id = sendItem.Id });
        }

        [HttpPost("/menu/delete")]
        public IActionResult DeleteItem(int id)
        {
            menuService.DeleteItem(id);
            return Ok();
        }

    }
}