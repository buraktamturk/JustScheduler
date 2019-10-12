using JustScheduler.SampleWeb.Models;
using JustScheduler.SampleWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace JustScheduler.SampleWeb.Controllers {
    public class HelloController : ControllerBase {
        private readonly IJobTrigger<MessageService, Message> trigger;
        
        public HelloController(IJobTrigger<MessageService, Message> trigger) {
            this.trigger = trigger;
        }

        [HttpGet("hello")]
        public string Hello(string message) {
            trigger.Trigger(new Message(message));
            return $"Message sent: {message}";
        }
    }
}