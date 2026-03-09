namespace FrankenTui.Runtime;

public interface IAppProgram<TModel, TMessage>
{
    TModel Initialize();

    UpdateResult<TModel, TMessage> Update(TModel model, TMessage message);

    IRuntimeView BuildView(TModel model);
}
