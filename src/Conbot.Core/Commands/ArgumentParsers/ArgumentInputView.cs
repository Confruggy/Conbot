using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;
using Disqord.Rest;

using Humanizer;

namespace Conbot.Commands
{
    public class ArgumentInputView : ViewBase
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public ConbotCommandContext Context { get; }
        public object[] Choices { get; }
        public string? Result { get; set; }
        public bool Skipped { get; set; }
        public bool Canceled { get; set; }

        public SelectionViewComponent? ChoiceSelection { get; }
        public ButtonViewComponent? SkipButton { get; }
        public ButtonViewComponent CancelButton { get; }

        public ArgumentInputView(LocalMessage templateMessage, ConbotCommandContext context,
            bool isOptional = false, object[]? choices = null)
            : base(templateMessage)
        {
            Context = context;
            Choices = choices ?? Array.Empty<object>();

            if (choices is not null)
            {
                List<LocalSelectionComponentOption> options = new();

                foreach (object choice in choices)
                {
                    string value = choice.ToString()!;
                    options.Add(new LocalSelectionComponentOption(value.ApplyCase(LetterCasing.Title), value));
                }

                ChoiceSelection = new SelectionViewComponent(OnChoiceSelectionAsync) { Options = options };

                AddComponent(ChoiceSelection);
            }

            if (isOptional)
            {
                SkipButton = new ButtonViewComponent(OnSkipButtonAsync)
                {
                    Label = "Skip",
                    Style = LocalButtonComponentStyle.Secondary
                };

                AddComponent(SkipButton);
            }

            CancelButton = new ButtonViewComponent(OnCancelButtonAsync)
            {
                Label = "Cancel",
                Style = LocalButtonComponentStyle.Danger
            };

            AddComponent(CancelButton);

            _cancellationTokenSource = new CancellationTokenSource();
        }

        public ValueTask OnChoiceSelectionAsync(SelectionEventArgs e)
        {
            Result = e.SelectedOptions[0].Value;
            DisableComponents();

            return default;
        }

        public ValueTask OnSkipButtonAsync(ButtonEventArgs e)
        {
            Skipped = true;
            DisableComponents();

            return default;
        }

        public ValueTask OnCancelButtonAsync(ButtonEventArgs e)
        {
            Canceled = true;
            DisableComponents();

            return default;
        }

        public override ValueTask UpdateAsync()
        {
            if (Skipped || Canceled || Result is not null)
            {
                _cancellationTokenSource.Cancel();
                Menu.Stop();
            }

            return default;
        }

        public void DisableComponents()
        {
            foreach (var component in EnumerateComponents())
            {
                if (component is ButtonViewComponent button)
                    button.IsDisabled = true;
                else if (component is SelectionViewComponent selection)
                    selection.IsDisabled = true;
            }
        }

        internal async Task BackgroundTask()
        {
            MessageReceivedEventArgs? e;
            try
            {
                e = await Context.WaitForMessageAsync(cancellationToken: _cancellationTokenSource.Token);
            }
            catch
            {
                e = null;
            }

            if (_cancellationTokenSource.IsCancellationRequested)
                return;

            if (e?.Message is IUserMessage message)
            {
                Result = message.Content;
                Context.AddMessage(message);
            }

            DisableComponents();

            if (Menu is DefaultMenu menu)
                await menu.ApplyChangesAsync();
        }
    }
}
