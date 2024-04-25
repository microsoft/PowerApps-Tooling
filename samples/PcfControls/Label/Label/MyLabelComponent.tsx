import * as React from 'react';
import { Label } from '@fluentui/react';

export interface IMyLabelComponentProps {
  text?: string;
}

export class MyLabelComponent extends React.Component<IMyLabelComponentProps> {
  public render(): React.ReactNode {
    return (
      <Label>
        [{this.props.text}]
      </Label>
    )
  }
}
