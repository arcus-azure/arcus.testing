import React from 'react';
import clsx from 'clsx';
import Translate from '@docusaurus/Translate';
import type {Props} from '@theme/Admonition/Type/Tip';
import AdmonitionLayout from '@theme/Admonition/Layout';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faAward } from '@fortawesome/free-solid-svg-icons'

const infimaClassName = 'alert alert--praise';

const defaultProps = {
  icon: <FontAwesomeIcon icon={faAward} />,
  title: (
    <Translate
      id="theme.admonition.praise"
      description="The default label used for the Praise admonition (:::praise)">
      praise
    </Translate>
  ),
};

export default function AdmonitionTypePraise(props: Props): JSX.Element {
  return (
    <AdmonitionLayout
      {...defaultProps}
      {...props}
      className={clsx(infimaClassName, props.className)}>
      {props.children}
    </AdmonitionLayout>
  );
}