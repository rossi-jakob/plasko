import { IToolbarButton } from '@/@types/FrmSettings';
import { openModalDialogEl } from '@/helpers';

export class ToolbarButtonEditDialogHtmlElement extends HTMLDialogElement {
  constructor() {
    super();

    // private methods
    this.openCreate = this.openCreate.bind(this);
    this.getDialogData = this.getDialogData.bind(this);
  }


  private connectedCallback() {
    this.innerHTML = `
      <form method="dialog">
        <header class="dialog-header">
          <span class="create-only" lang-text="FrmSettings.Toolbar._AddNewButton">[Add a custom toolbar button]</span>
          <span class="edit-only" lang-text="FrmSettings.Toolbar._EditButton">[Edit toolbar button]</span>
        </header>
        <div class="dialog-body" style="width: 40rem;">
          <div class="mb-3">
            <div class="mb-1" lang-text="FrmSettings.Toolbar._ButtonJson">[Button JSON]</div>
            <textarea class="w-100" name="_ButtonJson" required rows="20" spellcheck="false"
              style="font-family: var(--fontCode);"
              placeholder='{
  "Id": "Btn_OpenWithMSPaint",
  "Type": "Button",
  "Text": "Open with MS Paint",
  "Image": "C:\\\\path\\\\to\\\\icon.svg",
  "Alignment": "Left",
  "CheckableConfigBinding": "",
  "DisplayStyle": "Image",
  "OnClick": {
    "Executable": "mspaint.exe",
    "Arguments": ["<file>"],
    "NextAction": {
    }
  },
  "Hotkeys": []
}'></textarea>
          </div>
        </div>
        <footer class="dialog-footer">
          <button type="submit" lang-text="_._OK">[OK]</button>
          <button type="button" data-dialog-action="close" lang-text="_._Cancel">[Cancel]</button>
        </footer>
      </form>`;
  }


  /**
   * Opens tool dialog for create.
   */
  public async openCreate() {
    const defaultBtn = {
      Id: '',
      Type: 'Button',
      Text: '',
      Image: 'C:\\path\\to\\icon.svg',
      Alignment: 'Left',
      CheckableConfigBinding: '',
      DisplayStyle: 'Image',
      OnClick: {
        Executable: '',
        Arguments: ['<file>'],
      },
      Hotkeys: [],
    } as IToolbarButton;
    const json = JSON.stringify(defaultBtn, null, 2);

    const isSubmitted = await openModalDialogEl(this, 'create', { ButtonJson: json }, null,
      async () => {
        const data = this.getDialogData();
        const isValid = await postAsync<boolean>('Btn_AddCustomToolbarButton_ValidateJson_Create', data.ButtonJson);
        return isValid;
      });

    return isSubmitted;
  }


  /**
   * Opens tool dialog for edit.
   */
  public async openEdit(btn: IToolbarButton) {
    const json = JSON.stringify(btn, null, 2);

    const isSubmitted = await openModalDialogEl(this, 'edit', { ButtonJson: json }, null,
      async () => {
        const data = this.getDialogData();
        const isValid = await postAsync<boolean>('Btn_AddCustomToolbarButton_ValidateJson_Edit', data.ButtonJson);
        return isValid;
      });

    return isSubmitted;
  }


  /**
   * Gets data from the tool dialog.
   */
  public getDialogData() {
    // get data
    const json = query<HTMLTextAreaElement>('[name="_ButtonJson"]').value || '';

    return {
      ButtonJson: json.trim(),
    };
  }
}


/**
 * Creates and registers ToolbarButtonEditDialogHtmlElement to DOM.
 */
export const defineToolbarButtonEditDialogHtmlElement = () => window.customElements.define(
  'edit-toolbar-dialog',
  ToolbarButtonEditDialogHtmlElement,
  { extends: 'dialog' },
);
