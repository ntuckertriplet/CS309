﻿using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SelecTunes.Backend.Data;
using SelecTunes.Backend.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace SelecTunes.Backend.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PartyController : ControllerBase
    {
        private readonly ApplicationContext _context;

        private readonly UserManager<User> _userManager;

        private readonly ILogger<PartyController> _logger;

        public PartyController(ApplicationContext context, UserManager<User> userManager, ILogger<PartyController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public class JoinRequest
        {
            public string JoinCode { get; set; }
        }

        /**
         * Func JoinParty(string :joinCode) -> ActionResult<String>
         * => party join code
         * 
         * Send a POST request to /api/party/joinparty with a join code
         * The user that sent the request will be added to the party with that join code
         *
         * 15/02/2020 D/M/Y - Nathan Tucker - Stubbing
         * 09/03/2020 D/M/Y - Nathan Tucker - Finalizing
         */
        [HttpPost]
        [Authorize]
        public ActionResult<String> JoinParty([FromBody]JoinRequest join)
        {
            if (join == null)
            {
                return new BadRequestObjectResult("Party is null");
            }

            string joinCode = join.JoinCode;

            _logger.LogDebug("User {0} attempting to join party with join code {1}", _userManager.GetUserAsync(HttpContext.User).Result, joinCode);

            User ToJoin = _userManager.GetUserAsync(HttpContext.User).Result; // Find the current user asking to join a party
            Party party = _context.Parties.Where(p => p.JoinCode == joinCode).FirstOrDefault(); // Find a corresponding party with the requested join code

            if (party == null) // no party with that join code
            {
                return new NotFoundObjectResult("No party with that code exists");
            }

            if (party.KickedMembers.Contains(ToJoin))
            {
                return new ForbidResult("You have been banned from that party");
            }

            // In the event they are already in a party, remove them from that, add them to new party
            if (ToJoin.Party != null || ToJoin.PartyId != null)
            {
                if (ToJoin.Party.PartyHost == ToJoin) // uh oh, they are the host user
                {
                    DisbandCurrentParty(ToJoin.Party);
                }
                else
                {
                    ToJoin.Party.PartyMembers.Remove(ToJoin); // if they aren't the host, then go ahead and just do a simple remove
                }
                
                party.PartyMembers.Add(ToJoin);

                return new JsonResult(new { Success = true });
            }

            party.PartyMembers.Add(ToJoin);
            ToJoin.Party = party;

            _context.SaveChanges();

            return new JsonResult(new { Success = true });
        }

        /**
         * Func LeaveParty() -> ActionResult<String>
         *
         * Send a POST request to /api/party/leaveparty
         * The user that sent the request will be removed from the party which they are currently in
         *
         * 15/02/2020 D/M/Y - Nathan Tucker - Stubbing
         * 17/02/2020 D/M/Y - Alexander Young - Finalizing
         */
        [HttpPost]
        [Authorize]
        public ActionResult<String> LeaveParty()
        {
            _logger.LogDebug("User {0} attempting to leave party", _userManager.GetUserAsync(HttpContext.User).Result);

            User ToLeave = _userManager.GetUserAsync(HttpContext.User).Result;

            if (ToLeave == null)
            {
                return new UnauthorizedObjectResult("User needs to log in first");
            }

            Party PartyToLeave = _context.Parties.Where(p => p == ToLeave.Party || p.Id == ToLeave.PartyId).FirstOrDefault();

            if (PartyToLeave == null)
            {
                return new NotFoundObjectResult("No party with that code exists");
            }

            if (PartyToLeave.PartyHost == ToLeave && PartyToLeave.PartyHost.PartyId == PartyToLeave.Id)
            {
                if (DisbandCurrentParty(PartyToLeave))
                {
                    return new JsonResult(new { Success = true });
                }
                return new JsonResult(new { Success = false });
            }

            PartyToLeave.PartyMembers.Remove(ToLeave);

            _context.SaveChanges();

            return new JsonResult(new { Success = true });
        }

        /**
         * Func DisbandParty -> ActionResult<String>
         *
         * Send a DELTE request to /api/party/disband
         * 
         * The current party will be removed from the database
         * All of its members will be left partyless
         * The host will be logged out and removed
         *
         * 17/02/2020 - Nathan Tucker - Stubbing
         * 17/02/2020 - Alexander Young - Finalizing
         */
        [HttpDelete]
        [Authorize]
        public ActionResult<String> DisbandParty()
        {
            User PartyDisbander = _userManager.GetUserAsync(HttpContext.User).Result;

            if (PartyDisbander == null)
            {
                return new UnauthorizedObjectResult("Need to log in first");
            }

            Party PartyToDisband = _context.Parties.Where(p => p.Id == PartyDisbander.PartyId).FirstOrDefault();

            if (PartyToDisband == null)
            {
                return new NotFoundObjectResult("No party exists");
            }

            if (PartyToDisband.PartyHost != PartyDisbander)
            {
                return new ForbidResult("Not the host of the party!");
            }

            if (DisbandCurrentParty(PartyToDisband))
            {
                return new JsonResult(new { Success = true });
            }

            return new JsonResult(new { Success = false });
        }

        /**
         * Func Members -> ActionResult<String>
         *
         * Send a GET request to /api/party/members
         *
         * Gets a JSON array of all the current members of the party to populate the view
         *
         * 09/03/2020 - Nathan Tucker
         */
        [Authorize]
        [HttpGet]
        public ActionResult<String> Members()
        {
            User user = _userManager.GetUserAsync(HttpContext.User).Result;

            if (user == null)
            {
                return new UnauthorizedObjectResult("Need to log in first");
            }

            Party party = _context.Parties.Where(p => p.Id == user.PartyId).FirstOrDefault();

            if (party == null)
            {
                return new NotFoundObjectResult("You are not a member of a party");
            }

            // Temporary fix.
            List<User> wa = _context.Users.Where(u => u.PartyId == party.Id).ToList();

            List<PesudoUser> users = wa.Select(u => new PesudoUser { Email = u.Email }).ToList();
            return Ok(users);
        }

        internal class PesudoUser
        {
            public string Email { get; set; }
        }

        /**
         * Func ToggleExplicit() -> ActionResult<String>
         *
         * Send a POST request to the endpoint to enable or disable explicit song searches
         * 
         * Returns a success based on if it succeeded
         *
         * 05/04/2020 - Nathan Tucker
         */
        [Authorize]
        [Route("/api/party/explicit")]
        [HttpPost]
        public ActionResult<String> ToggleExplicit()
        {
            User user = _userManager.GetUserAsync(HttpContext.User).Result;

            if (user == null)
            {
                return new UnauthorizedObjectResult("User needs to log in first");
            }

            Party party = _context.Parties.Where(p => p.Id == user.PartyId).FirstOrDefault();

            if (party == null)
            {
                return new NotFoundObjectResult("User is not in a party");
            }

            party.AllowExplicit = !party.AllowExplicit;

            _context.SaveChanges();

            return new JsonResult(new { Success = true });
        }

        /**
         * Func ViewExplicit() -> ActionResult<String>
         * => true
         *
         * Send a GET request to the endpoint to view the current status of explicit song searches
         * This will be true if allowed, false otherwise
         *
         * 05/04/2020 - Nathan Tucker
         */
        [Authorize]
        [Route("/api/party/explicit")]
        [HttpGet]
        public ActionResult<String> ViewExplicit()
        {
            User user = _userManager.GetUserAsync(HttpContext.User).Result;

            if (user == null)
            {
                return new UnauthorizedObjectResult("User needs to log in");
            }

            Party party = _context.Parties.Where(p => p.Id == user.PartyId).FirstOrDefault();

            if (party == null)
            {
                return new NotFoundObjectResult("User is not in a party that exists");
            }

            return new JsonResult(new { allowed = party.AllowExplicit});
        }

        /**
         * Func DisbandCurrentParty(Party: PartyToDisband) -> bool
         * => true
         *
         * Method that would get repeated in many instances.
         * If the current user tries to leave the party and is host or disband the party
         *  this method gets called
         */
        private bool DisbandCurrentParty(Party PartyToDisband)
        {
            if (PartyToDisband == null)
            {
                return false;
            }

            PartyToDisband.PartyMembers.Clear();
            PartyToDisband.KickedMembers.Clear();

            try { 
                _context.SaveChanges();
            } 
            catch (DbUpdateException)
            {
                _logger.LogError("Error disbanding party");
                return false;
            }

            _context.Parties.Remove(PartyToDisband);

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError("Error disbanding party");
                Console.WriteLine(e.StackTrace);
                return false;
            }
            
            return true;
        }
    }
}
