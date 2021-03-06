﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Evento.Core.Domain
{
    public class Event : Entity
    {
        private ISet<Ticket> _tickets = new HashSet<Ticket>();


        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public DateTime StartDate { get; protected set; }
        public DateTime EndDate { get; protected set; }
        public DateTime UpdatedAt { get; protected set; }

        public IEnumerable<Ticket> Tickets => _tickets;
        public IEnumerable<Ticket> PurchasdTickets => Tickets.Where(x => x.Purchased);
        //public IEnumerable<Ticket> PurchasdTickets => Tickets.Where(x => !x.Purchased);
        public IEnumerable<Ticket> AvaliableTickets => Tickets.Except(PurchasdTickets);



        protected Event()
        {
        }

        public Event(Guid id, string name, string description, DateTime startDate, DateTime endDate)
        {
            Id = id;
            SetName(name);
            SetDescription(description);
            SetDate(startDate, endDate);
            EndDate = endDate;
            CreatedAt = DateTime.UtcNow; 
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetDate(DateTime startDate, DateTime endDate)
        {
            if(startDate>=endDate)
            {
                throw new Exception($"Event with id: {Id} must have a date greater than start date.");
            }
            StartDate = startDate;
            EndDate = endDate;
        }

        public void SetName(string name)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                throw new Exception($"Event with id: '{Id}' can not have an empty name.");
            }
            Name = name;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetDescription(string description)
        {
            if(string.IsNullOrWhiteSpace(description))
            {
                throw new Exception($"Event with id: '{Id}' can not have an empty name.");
            }
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }



        public void AddTickets (int amount, decimal price)
        {
            var seating = _tickets.Count + 1;
            for(var i=0; i<amount; i++)
            {
                _tickets.Add(new Ticket(this, seating, price));
                seating++;
            }
        }

        public void PurchaseTickets(User user, int amount)
        {
            if(AvaliableTickets.Count()< amount)
            {
                throw new Exception($"Not enough avaliable tickets to purchase ({amount}) by user: '{user.Name}'.");
            }
            var tickets = AvaliableTickets.Take(amount);
            foreach(var ticket in tickets)
            {
                ticket.Purchase(user);
            }
        }

        public void CancelPurchasedTickets(User user, int amount)
        {
            var tickets = GetTicketsPurchasedByUser(user);
            if (tickets.Count() < amount)
            {
                throw new Exception($"Not enough purchased tickets to be canceled ({amount}) by user: '{user.Name}'");
            }
            foreach(var ticket in tickets.Take(amount))
            {
                ticket.Cancel();
            }
        }

        public IEnumerable<Ticket> GetTicketsPurchasedByUser(User user)
            => PurchasdTickets.Where(x => x.UserId == user.Id);

    }
}
